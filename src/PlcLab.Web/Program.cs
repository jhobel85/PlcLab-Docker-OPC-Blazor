
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.OPC;
using PlcLab.Web;
using Serilog;
using Serilog.Extensions.Logging;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(); // Use Serilog for logging
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<ITelemetryContext>(_ => SerilogTelemetry.Create());
builder.Services.AddSingleton<IOpcUaClientFactory, OpcUaClientFactory>();
// Register demo data seeder hosted service
builder.Services.AddHostedService<PlcLab.Infrastructure.DemoDataSeederHostedService>();
var app = builder.Build();
app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
