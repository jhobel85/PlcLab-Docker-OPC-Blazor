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

        // Recursively browse all nodes and log their details        
        private async Task RecursiveBrowseAsync(Opc.Ua.Client.Session session, NodeId nodeId, CancellationToken ct, string indent)
        {
            var children = await _opcFactory.BrowseAsync(session, nodeId, ct);
            foreach (var child in children)
            {
                Log.Information("{Indent}- DisplayName: {DisplayName}, BrowseName: {BrowseName}, NodeId: {NodeId}, NodeClass: {NodeClass}",
                    indent, child.DisplayName.Text, child.BrowseName.ToString(), child.NodeId, child.NodeClass);
                // Recurse into child nodes that are Objects or Folders
                if (child.NodeClass == NodeClass.Object || child.NodeClass == NodeClass.Variable || child.NodeClass == NodeClass.Method)
                {
                    var childNodeId = ExpandedNodeId.ToNodeId(child.NodeId, session.NamespaceUris);
                    await RecursiveBrowseAsync(session, childNodeId, ct, indent + "  ");
                }
            }
        }

        private async Task SeedDemoDataAsync(Opc.Ua.Client.Session session, CancellationToken cancellationToken)
        {
            try
            {
                // Recursively browse and log all nodes starting from RootFolder - takes long time
                /*
                Log.Information("Starting recursive browse of OPC UA address space...");
                await RecursiveBrowseAsync(session, Opc.Ua.ObjectIds.RootFolder, cancellationToken, "");
                Log.Information("Recursive browse completed.");
                */                            

                // Use available demo variables from the reference server
                var demoVariables = new[]
                {
                    new { Label = "Process/State", Path = "ReferenceTest/Scalar/Scalar_Static/Boolean" },
                    new { Label = "Analog/Flow", Path = "ReferenceTest/Scalar/Scalar_Static/Double" },
                    new { Label = "Digital/ValveOpen", Path = "ReferenceTest/Scalar/Scalar_Static/Boolean" }
                };
                foreach (var variable in demoVariables)
                {
                    try
                    {
                        // Walk the path from Objects
                        var parts = variable.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        var currentId = new NodeId(85); // Objects
                        foreach (var part in parts)
                        {
                            var children = await _opcFactory.BrowseAsync(session, currentId, cancellationToken);
                            Log.Information("Browsing children of NodeId: {NodeId} for path part: {Part}", currentId, part);
                            foreach (var child in children)
                            {
                                Log.Information("  - DisplayName: {DisplayName}, BrowseName: {BrowseName}, NodeId: {NodeId}, NodeClass: {NodeClass}", child.DisplayName.Text, child.BrowseName.ToString(), child.NodeId, child.NodeClass);
                            }
                            var match = children.FirstOrDefault(r => r.DisplayName.Text == part);
                            if (match == null)
                            {
                                Log.Warning("Path part '{Part}' not found among children of NodeId {NodeId}", part, currentId);
                                break;
                            }
                            currentId = ExpandedNodeId.ToNodeId(match.NodeId, session.NamespaceUris);
                        }

                        var nodeId = currentId;
                        var value = await _opcFactory.ReadValueAsync(session, nodeId, cancellationToken);
                        Log.Information("Found demo variable {Label} at {Path} (NodeId: {NodeId}, Value: {Value})", variable.Label, variable.Path, nodeId, value);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("Demo variable {Label} at {Path} not found or error reading: {Message}", variable.Label, variable.Path, ex.Message);
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
