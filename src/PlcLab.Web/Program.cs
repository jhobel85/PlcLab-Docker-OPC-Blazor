
using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.OPC;
using PlcLab.Web;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog(); // Use Serilog for logging
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSingleton<ITelemetryContext>(_ => SerilogTelemetry.Create());
builder.Services.AddSingleton<IOpcUaClientFactory, OpcUaClientFactory>();
// Register demo data seeder hosted service and as injectable singleton
builder.Services.AddSingleton<PlcLab.Infrastructure.DemoDataSeederHostedService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<PlcLab.Infrastructure.DemoDataSeederHostedService>());
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
