
using PlcLab.Web.Api;
using PlcLab.Infrastructure;
using PlcLab.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Opc.Ua;
using PlcLab.OPC;
using PlcLab.Web;
using Serilog;
using PlcLab.Web.Services;
using PlcLab.Web.ViewModel;
using PlcLab.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PlcLab.Application.Ports;

// Configure Serilog before building
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
// Add JWT Bearer authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"] ?? "https://demo.identityserver.io/";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();
// Add SQL Server DbContext
// Add PostgreSQL DbContext
builder.Services.AddDbContext<PlcLabDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Host.UseSerilog(); // Use Serilog for logging
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddHttpClient("Api", client =>
{
    // Set the base address for API calls (robust fallback)
    static bool TryGetValidUri(string? value, out Uri? uri)
    {
        // Reject null/whitespace and wildcards like "+" or "*" (common in ASPNETCORE_URLS)
        if (string.IsNullOrWhiteSpace(value) || value.Contains("+") || value.Contains("*"))
        {
            uri = null;
            return false;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out uri) && !string.IsNullOrWhiteSpace(uri.Host);
    }

    var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
    var aspnetcoreUrls = builder.Configuration["ASPNETCORE_URLS"];

    if (!TryGetValidUri(apiBaseUrl, out var baseUri) && !TryGetValidUri(aspnetcoreUrls, out baseUri))
    {
        baseUri = new Uri("http://localhost:8080/");
    }

    client.BaseAddress = baseUri!;
});
builder.Services.AddSingleton<ITelemetryContext>(_ => SerilogTelemetry.Create());
builder.Services.Configure<MockOpcUaOptions>(builder.Configuration.GetSection("MockOpcUa"));
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MockOpcUaOptions>>().Value;
    return new PlcLab.OPC.Mock.MockOpcUaServer(opts.Endpoint);
});
// Temporarily disable mock server hosted service to avoid port conflicts in tests
// builder.Services.AddHostedService<MockOpcUaHostedService>();
// Register OPC adapters for Application ports
builder.Services.AddSingleton<PlcLab.OPC.Adapters.OpcSessionAdapter>();
builder.Services.AddSingleton<PlcLab.OPC.Adapters.OpcBrowseAdapter>();
builder.Services.AddSingleton<PlcLab.OPC.Adapters.OpcReadWriteAdapter>();
builder.Services.AddSingleton<PlcLab.OPC.Adapters.OpcSubscriptionAdapter>();
builder.Services.AddSingleton<PlcLab.Infrastructure.BrowseService>();
builder.Services.AddSingleton<PlcLab.Infrastructure.Services.ISeederService, PlcLab.Infrastructure.SeederHostedService>();
builder.Services.AddSingleton<PlcLab.Infrastructure.SeederHostedService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var sessionPort = sp.GetRequiredService<PlcLab.Application.Ports.IOpcSessionPort>();
    var browseService = sp.GetRequiredService<PlcLab.Infrastructure.BrowseService>();
    return new PlcLab.Infrastructure.SeederHostedService(config, sessionPort, browseService);
});
builder.Services.AddSingleton<ILiveSignalSubscriptionService, LiveSignalSubscriptionService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<PlcLab.Infrastructure.SeederHostedService>());
builder.Services.AddScoped<IndexViewModel>();
builder.Services.AddScoped<OpcUaEndpointService>();
builder.Services.AddSingleton<IOpcConnectionService, ConnectionStatusService>();
builder.Services.AddScoped<ISeedDataClient, SeedDataClient>();
// Register Application ports for orchestrator and other consumers
builder.Services.AddSingleton<PlcLab.Application.Ports.IOpcSessionPort>(sp => sp.GetRequiredService<PlcLab.OPC.Adapters.OpcSessionAdapter>());
builder.Services.AddSingleton<PlcLab.Application.Ports.IBrowsePort>(sp => sp.GetRequiredService<PlcLab.OPC.Adapters.OpcBrowseAdapter>());
builder.Services.AddSingleton<PlcLab.Application.Ports.IReadWritePort>(sp => sp.GetRequiredService<PlcLab.OPC.Adapters.OpcReadWriteAdapter>());
builder.Services.AddSingleton<PlcLab.Application.Ports.ISubscriptionPort>(sp => sp.GetRequiredService<PlcLab.OPC.Adapters.OpcSubscriptionAdapter>());
builder.Services.AddScoped<TestRunOrchestrator>();

// Minimal IOpcUaSessionFactory implementation for DI
builder.Services.AddSingleton<IOpcUaSessionFactory>(sp =>
{
    var sessionPort = sp.GetRequiredService<PlcLab.Application.Ports.IOpcSessionPort>();
    return new OpcUaSessionFactoryImpl(sessionPort);
});

// OpenTelemetry tracing configuration
builder.Services.AddPlcLabOpenTelemetry(builder.Configuration);
var app = builder.Build();

// Automatically apply EF Core migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlcLab.Infrastructure.PlcLabDbContext>();
    db.Database.Migrate();
}
// Register API endpoints
SeedInfoApi.MapSeedInfoEndpoint(app);
TestPlansApi.MapTestPlansApi(app);
TestRunsApi.MapTestRunsApi(app);
app.UseHttpsRedirection();
// app.UseAuthentication();
// app.UseAuthorization();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
