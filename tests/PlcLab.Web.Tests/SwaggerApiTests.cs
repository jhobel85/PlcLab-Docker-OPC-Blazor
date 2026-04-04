using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PlcLab.Web.Api;
using PlcLab.Web.OpenApi;
using PlcLab.Web.Services;
using Xunit;

namespace PlcLab.Web.Tests;

public class SwaggerApiTests
{
    [Fact]
    public async Task SwaggerUi_IsAvailable_InDevelopment()
    {
        await using var app = await CreateTestAppAsync(Environments.Development);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Swagger UI", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenApiJson_IsAvailable_InDevelopment()
    {
        await using var app = await CreateTestAppAsync(Environments.Development);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SwaggerUi_IsNotAvailable_OutsideDevelopment()
    {
        await using var app = await CreateTestAppAsync(Environments.Production);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_Contains_AllApiAndHealthEndpoints()
    {
        await using var app = await CreateTestAppAsync(Environments.Development);
        using var client = app.GetTestClient();

        using var doc = await LoadOpenApiAsync(client);
        var normalizedPaths = doc.RootElement
            .GetProperty("paths")
            .EnumerateObject()
            .Select(path => NormalizePath(path.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("/api/certificates", normalizedPaths);
        Assert.Contains("/api/certificates/promote", normalizedPaths);
        Assert.Contains("/api/certificates/reject", normalizedPaths);
        Assert.Contains("/api/certificates/rejected/{fileName}", normalizedPaths);
        Assert.Contains("/api/seedinfo", normalizedPaths);
        Assert.Contains("/api/testplans", normalizedPaths);
        Assert.Contains("/api/testplans/{id}", normalizedPaths);
        Assert.Contains("/api/testruns", normalizedPaths);
        Assert.Contains("/api/testruns/{id}", normalizedPaths);
        Assert.Contains("/health", normalizedPaths);
        Assert.Contains("/healthz", normalizedPaths);
    }

    [Fact]
    public async Task OpenApiDocument_Contains_BearerSecurityScheme()
    {
        await using var app = await CreateTestAppAsync(Environments.Development);
        using var client = app.GetTestClient();

        using var doc = await LoadOpenApiAsync(client);
        var bearer = doc.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");

        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task AuthorizedSeedInfoOperation_HasSecurityRequirement()
    {
        await using var app = await CreateTestAppAsync(Environments.Development);
        using var client = app.GetTestClient();

        using var doc = await LoadOpenApiAsync(client);
        var getSeedInfo = doc.RootElement
            .GetProperty("paths")
            .GetProperty("/api/seedinfo")
            .GetProperty("get");

        Assert.True(getSeedInfo.TryGetProperty("security", out var security));
        Assert.True(security.GetArrayLength() > 0);
    }

    private static async Task<JsonDocument> LoadOpenApiAsync(HttpClient client)
    {
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        return JsonDocument.Parse(json);
    }

    private static string NormalizePath(string path) =>
        path.Replace(":guid}", "}", StringComparison.OrdinalIgnoreCase);

    private static async Task<WebApplication> CreateTestAppAsync(string environmentName)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName,
            ContentRootPath = Directory.GetCurrentDirectory()
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddHealthChecks();
        builder.Services.AddScoped<CertificatesService>();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = "https://demo.identityserver.io/";
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = false
                };
            });
        builder.Services.AddAuthorization();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "PlcLab API", Version = "v1" });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            options.OperationFilter<BearerSecurityOperationFilter>();
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithTags("Health")
            .WithName("GetHealth")
            .WithSummary("Returns a lightweight liveness response.");
        app.MapGet("/healthz", async (
                [Microsoft.AspNetCore.Mvc.FromServices] Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService,
                CancellationToken cancellationToken) =>
            {
                var report = await healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
                var statusCode = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status200OK;

                return Results.Text(report.Status.ToString(), "text/plain", statusCode: statusCode);
            })
            .WithTags("Health")
            .WithName("GetHealthReadiness")
            .WithSummary("Returns the ASP.NET Core readiness health status.");

        CertificatesApi.MapCertificatesApi(app);
        SeedInfoApi.MapSeedInfoEndpoint(app);
        TestPlansApi.MapTestPlansApi(app);
        TestRunsApi.MapTestRunsApi(app);

        await app.StartAsync();
        return app;
    }
}