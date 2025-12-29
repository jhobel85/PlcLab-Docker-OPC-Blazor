
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.OPC;
using PlcLab.Web;
using Serilog;
using Serilog.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

//The OpcUaClientFactory receives its ITelemetryContext parameter through Dependency Injection (DI).
builder.Services.AddSingleton(sp =>
{
    var msLoggerFactory = new SerilogLoggerFactory(Log.Logger, dispose: false);
    // Reuse the SerilogTelemetry helper if you prefer:
    return (ITelemetryContext)Activator.CreateInstance(
        typeof(SerilogTelemetry),
        [msLoggerFactory] // <-- args
    )!;
});

//builder.Services.AddSingleton<ISessionFactory, DefaultSessionFactory>();
builder.Services.AddSingleton<IOpcUaClientFactory, OpcUaClientFactory>();
var app = builder.Build();
app.UseStaticFiles();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
