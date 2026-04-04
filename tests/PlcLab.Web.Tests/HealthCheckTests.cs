using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Allure.Xunit.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace PlcLab.Web.Tests;

[AllureSuite("HealthCheck")]
public class HealthCheckTests
{
    // ── /healthz (ASP.NET Core Health Checks) ─────────────────────────────────

    [Fact]
    [AllureFeature("Readiness")]
    public async Task Healthz_Returns200_WhenAllChecksPass()
    {
        await using var app = await CreateTestAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [AllureFeature("Readiness")]
    public async Task Healthz_ReturnsHealthyBody()
    {
        await using var app = await CreateTestAppAsync();
        using var client = app.GetTestClient();

        var body = await client.GetStringAsync("/healthz");

        Assert.Equal("Healthy", body, ignoreCase: true);
    }

    [Fact]
    [AllureFeature("Readiness")]
    public async Task Healthz_ContentType_IsTextPlain()
    {
        await using var app = await CreateTestAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/healthz");

        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Contains("text/plain", response.Content.Headers.ContentType!.MediaType);
    }

    // ── /health (legacy JSON ping) ────────────────────────────────────────────

    [Fact]
    [AllureFeature("Liveness")]
    public async Task Health_Returns200_WithStatusOk()
    {
        await using var app = await CreateTestAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("ok", doc.RootElement.GetProperty("status").GetString());
    }

    // ── Degraded check propagation ────────────────────────────────────────────

    [Fact]
    [AllureFeature("Readiness")]
    public async Task Healthz_Returns503_WhenCheckIsUnhealthy()
    {
        await using var app = await CreateTestAppAsync(unhealthy: true);
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/healthz");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<WebApplication> CreateTestAppAsync(bool unhealthy = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
            ContentRootPath = System.IO.Directory.GetCurrentDirectory()
        });

        builder.WebHost.UseTestServer();

        var hcBuilder = builder.Services.AddHealthChecks();
        if (unhealthy)
            hcBuilder.AddCheck("always-unhealthy", () => HealthCheckResult.Unhealthy("Forced failure"));

        var app = builder.Build();

        app.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Ok(new { status = "ok" }));
        app.MapHealthChecks("/healthz");

        await app.StartAsync();
        return app;
    }
}
