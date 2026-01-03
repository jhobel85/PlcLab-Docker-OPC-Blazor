using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using PlcLab.OPC;
using Opc.Ua;

namespace PlcLab.Infrastructure
{
    public class DemoDataSeederHostedService(IConfiguration configuration, IOpcUaClientFactory opcFactory) : IHostedService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IOpcUaClientFactory _opcFactory = opcFactory;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_configuration.GetValue<bool>("Seed:Enabled"))
            {
                Log.Information("Demo data seeding is disabled (Seed:Enabled=false)");
                return;
            }

            Log.Information("Seeding demo OPC UA nodes and methods...");

            var endpoint = _configuration.GetValue<string>("OpcUa:Endpoint") ?? "opc.tcp://localhost:4840";

            // Retry logic: OPC UA server may not be ready immediately on startup
            const int maxRetries = 10;
            const int delayMs = 3000;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var session = await _opcFactory.CreateSessionAsync(endpoint, useSecurity: false, cancellationToken);
                    Log.Information("Connected to OPC UA server at {Endpoint} (attempt {Attempt}/{MaxRetries})", endpoint, attempt, maxRetries);
                    
                    await SeedDemoDataAsync(session, cancellationToken);
                    Log.Information("Demo data seeding completed");
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Log.Warning("Failed to connect to OPC UA server (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {DelayMs}ms...", 
                        attempt, maxRetries, ex.Message, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to seed demo data after {MaxRetries} attempts: {Message}", maxRetries, ex.Message);
                    return;
                }
            }
        }

        private async Task SeedDemoDataAsync(Opc.Ua.Client.Session session, CancellationToken cancellationToken)
        {
            try
            {

                // Check for demo variables
                var demoVariables = new[] { "Process/State", "Analog/Flow", "Digital/ValveOpen" };
                foreach (var path in demoVariables)
                {
                    try
                    {
                        var nodeId = await _opcFactory.ResolveNodeIdAsync(session, path, cancellationToken);
                        var value = await _opcFactory.ReadValueAsync(session, nodeId, cancellationToken);
                        Log.Information("Found demo variable {Path} (NodeId: {NodeId}, Value: {Value})", path, nodeId, value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Demo variable {Path} not found or error reading: {Message}", path, ex.Message);
                    }
                }

                // Check for demo methods
                var demoMethods = new[] { "StartSequenceTest", "ResetAlarms" };
                foreach (var methodName in demoMethods)
                {
                    try
                    {
                        var nodeId = await _opcFactory.ResolveNodeIdAsync(session, methodName, cancellationToken);
                        Log.Information("Found demo method {MethodName} (NodeId: {NodeId})", methodName, nodeId);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Demo method {MethodName} not found: {Message}", methodName, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during demo data seeding: {Message}", ex.Message);
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
