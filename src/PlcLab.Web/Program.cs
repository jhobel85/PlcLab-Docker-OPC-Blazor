using PlcLab.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.OPC;
using PlcLab.Web;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using PlcLab.Web.Services;

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
builder.Services.AddScoped<PlcLab.Web.Pages.IndexViewModel>();
builder.Services.AddSingleton<PlcLab.Infrastructure.SeederHostedService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<PlcLab.Infrastructure.SeederHostedService>());

// OpenTelemetry tracing configuration
builder.Services.AddPlcLabOpenTelemetry(builder.Configuration);
var app = builder.Build();
// Register API endpoint for seed info
PlcLab.Web.Services.SeedInfoApi.MapSeedInfoEndpoint(app);
app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
