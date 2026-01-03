using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Opc.Ua;
using PlcLab.OPC;

namespace PlcLab.Web;

public static class SeedInfoApi
{
    public static void MapSeedInfoEndpoint(this WebApplication app)
    {
        app.MapGet("/api/seedinfo", async (IConfiguration config, IOpcUaClientFactory opcFactory) =>
        {
            var seedEnabled = config.GetValue<bool>("Seed:Enabled");
            if (!seedEnabled)
                return Results.Ok(new { seedEnabled, variables = Array.Empty<object>(), debug = "Seeding disabled" });

            var endpoint = config.GetValue<string>("OpcUa:Endpoint") ?? "opc.tcp://localhost:4840";
            var demoVariables = new[]
            {
                new { Label = "Process/State", Path = "ReferenceTest/Scalar/Scalar_Static/Boolean" },
                new { Label = "Analog/Flow", Path = "ReferenceTest/Scalar/Scalar_Static/Double" },
                new { Label = "Digital/ValveOpen", Path = "ReferenceTest/Scalar/Scalar_Static/Boolean" }
            };
            var results = new List<object>();
            var debugInfo = new List<string>();
            try
            {
                using var session = await opcFactory.CreateSessionAsync(endpoint, useSecurity: false);
                debugInfo.Add($"Connected to OPC UA endpoint: {endpoint}");
                foreach (var variable in demoVariables)
                {
                    try
                    {
                        var parts = variable.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                        var currentId = new Opc.Ua.NodeId(85); // Objects
                        debugInfo.Add($"\nBrowsing for variable '{variable.Label}' with path: {variable.Path}");
                        foreach (var part in parts)
                        {
                            var children = await opcFactory.BrowseAsync(session, currentId);
                            debugInfo.Add($"  Browsing node {currentId}: found {children.Count} children");
                            var match = children.FirstOrDefault(r => r.DisplayName.Text == part);
                            if (match == null)
                            {
                                debugInfo.Add($"    Part '{part}' not found among children of node {currentId}");
                                currentId = null;
                                break;
                            }
                            debugInfo.Add($"    Found part '{part}' as node {match.NodeId}");
                            currentId = Opc.Ua.ExpandedNodeId.ToNodeId(match.NodeId, session.NamespaceUris);
                        }
                        if (currentId != null)
                        {
                            object value = null;
                            try
                            {
                                value = await opcFactory.ReadValueAsync(session, currentId);
                            }
                            catch (Exception ex)
                            {
                                debugInfo.Add($"    Exception reading value: {ex.Message}");
                            }
                            debugInfo.Add($"    Read value: {value ?? "<null>"} (type: {value?.GetType().Name ?? "null"})");
                            results.Add(new { variable.Label, variable.Path, Value = value?.ToString() ?? "<null>" });
                        }
                        else
                        {
                            debugInfo.Add($"    Could not resolve full path for variable '{variable.Label}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        debugInfo.Add($"    Exception for variable '{variable.Label}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                debugInfo.Add($"Exception during OPC UA session or browse: {ex.Message}");
            }
            return Results.Ok(new { seedEnabled, variables = results, debug = string.Join("\n", debugInfo) });
        });
    }
}
