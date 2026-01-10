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

// Configure Serilog before building
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddSingleton<IOpcUaClientFactory, OpcUaClientFactory>();
builder.Services.AddSingleton<ILiveSignalSubscriptionService, LiveSignalSubscriptionService>();
// Register demo data seeder hosted service and as injectable singleton
builder.Services.AddScoped<IndexViewModel>();
builder.Services.AddSingleton<PlcLab.Infrastructure.SeederHostedService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<PlcLab.Infrastructure.SeederHostedService>());
// Register OPC UA endpoint service as scoped - (if singleton we getannot consume scoped service 'Microsoft.JSInterop.IJSRuntime')
builder.Services.AddScoped<OpcUaEndpointService>();
// Register connection status service as singleton to share connection state across all pages
builder.Services.AddSingleton<ConnectionStatusService>();

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
//app.UseAuthentication();
//app.UseAuthorization();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
