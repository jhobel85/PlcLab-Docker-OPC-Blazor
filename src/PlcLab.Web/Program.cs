using PlcLab.Infrastructure;
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
builder.Services.AddDbContext<PlcLabDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Host.UseSerilog(); // Use Serilog for logging
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<ITelemetryContext>(_ => SerilogTelemetry.Create());
builder.Services.AddSingleton<IOpcUaClientFactory, OpcUaClientFactory>();
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
// Register API endpoint for seed info
SeedInfoApi.MapSeedInfoEndpoint(app);
app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
