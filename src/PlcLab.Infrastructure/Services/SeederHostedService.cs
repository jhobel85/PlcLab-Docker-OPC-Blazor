using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Gds;
using PlcLab.Infrastructure;
using PlcLab.OPC;
using Serilog;

namespace PlcLab.Infrastructure
{
    public class SeederHostedService : IPlcService<Session, SeedInfo>
    {
        private readonly IConfiguration _configuration;
        private readonly IOpcUaClientFactory _opcFactory;
        private readonly BrowseService _browseService;

        public SeederHostedService(IConfiguration configuration, IOpcUaClientFactory opcFactory, BrowseService browseService)
        {
            _configuration = configuration;
            _opcFactory = opcFactory;
            _browseService = browseService;
        }

        private SeedInfo? _seedInfo;
        private Session? _session;

        public async Task<Session> GetSessionAsync(CancellationToken cancellationToken)
        {
            if (_session != null && _session.Connected)
            {
                Log.Debug("Reusing existing OPC UA session.");
                return _session;
            }

            if (!_configuration.GetValue<bool>("Seed:Enabled"))
            {
                Log.Information("Demo data seeding is disabled (Seed:Enabled=false)");
                return null;
            }

            Log.Information("Seeding demo OPC UA nodes and methods...");

            var endpoint = _configuration.GetValue<string>("OpcUa:Endpoint") ?? PlcLabConstants.DefaultOpcUaEndpoint;

            // Retry logic: OPC UA server may not be ready immediately on startup
            const int maxRetries = PlcLabConstants.DefaultMaxRetries;
            const int delayMs = PlcLabConstants.DefaultRetryDelayMs;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _session = await _opcFactory.CreateSessionAsync(endpoint, useSecurity: false, cancellationToken);
                    Log.Information("Connected to OPC UA server at {Endpoint} (attempt {Attempt}/{MaxRetries})", endpoint, attempt, maxRetries);
                    return _session;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Log.Warning("Failed to connect to OPC UA server (attempt {Attempt}/{MaxRetries}): {Message}. Retrying in {DelayMs}ms...",
                        attempt, maxRetries, ex.Message, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
            }

            throw new Exception($"Unable to connect to OPC UA server at {endpoint} after {maxRetries} attempts.");
        }

        public async Task<SeedInfo> GetDataAsync(Session session, CancellationToken cancellationToken)
        {
            _seedInfo = await SeedDemoDataAsync(session, cancellationToken);
            return _seedInfo;
        }


///InvalidCastException, InvalidOperationException if wrong conversion type is used -> caller must handle it.
        public async Task<TResult> CallMethodAsync<TResult>(Session session, string methodName, params object[] args)
        {
            if (methodName.Equals("Add", StringComparison.OrdinalIgnoreCase) && args.Length == 2 &&
                args[0] is float num1 && args[1] is uint num2)
            {
                var result = await CallMethodAsync_Add(session, num1, num2);
                return (TResult)Convert.ChangeType(result, typeof(TResult));
            }
            throw new NotImplementedException($"Method {methodName} is not implemented in SeederHostedService.");
        }

       private async Task<SeedInfo> SeedDemoDataAsync(Session session, CancellationToken cancellationToken)
        {
            if (session == null)
                throw new InvalidOperationException("Not connected to OPC UA server.");

            try
            {
                var nodeIds = SeedDemoData.Variables.Select(v => new NodeId(v.NodeId)).ToList();
                DataValue[]? values = null;
                if (nodeIds.Count > 0)
                {
                    var readResult = await session.ReadValuesAsync(nodeIds, cancellationToken);
                    values = readResult.Item1?.ToArray();
                }

                // Log results and build SeedInfo
                var seedVariables = new List<SeedVariable>();
                for (int i = 0; i < SeedDemoData.Variables.Length; i++)
                {
                    var label = SeedDemoData.Variables[i].Label;
                    var nodeId = nodeIds[i].ToString();
                    var value = values != null && i < values.Length ? values[i].Value : null;
                    Log.Information("Found demo variable {Label} (NodeId: {NodeId}, Value: {Value}, Type: {Type})", label, nodeId, value, value?.GetType().Name ?? "null");
                    seedVariables.Add(new SeedVariable
                    {
                        Label = label,
                        NodeId = nodeId,
                        Value = value,
                        ValueType = value?.GetType().Name ?? "null"
                    });
                }
                _seedInfo = new SeedInfo
                {
                    SeedEnabled = true,
                    Variables = seedVariables
                };
                return _seedInfo;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during demo data seeding: {Message}", ex.Message);
                throw;
            }
        }

        private async Task<double?> CallMethodAsync_Add(Session? session, float num1, uint num2)
        {
            if (session == null)
                throw new InvalidOperationException("Not connected to OPC UA server.");

            // Use NodeId from DemoDataVariables.Methods
            var methodEntry = SeedDemoData.Methods.FirstOrDefault(m => m.Label == "Add");
            if (methodEntry.NodeId == null)
                throw new Exception("Add method NodeId not defined in DemoDataVariables.Methods.");
            var methodId = new NodeId(methodEntry.NodeId);

            // The objectId is typically the parent node of the method node. If you know it, you can hardcode it; otherwise, fetch as before.
            var objectId = await _browseService.GetParentNodeIdAsync(session, methodId);

            if (objectId == null || methodId == null)
            {
                Log.Error("Cannot find object or method {ObjectId}, {MethodId}", objectId, methodId);
                return null;
            }
            else
            {
                Log.Debug("Calling Add method:"); // Serilog debug output
                Log.Debug("  objectId: {ObjectId}", objectId);
                Log.Debug("  methodId: {MethodId}", methodId);
                Log.Debug("  num1: {Num1} (type: {Num1Type})", num1, num1.GetType());
                Log.Debug("  num2: {Num2} (type: {Num2Type})", num2, num2.GetType());

                var inputArgs = new Variant[] { new Variant(num1), new Variant(num2) };
                Log.Debug("  inputArgs: [{InputArgs}]", string.Join(", ", inputArgs.Select(a => $"{a.Value} ({a.Value?.GetType()})")));

                // Call the method using the factory and handle output
                try
                {
                    var output = await _opcFactory.CallMethodAsync(session, objectId, methodId, inputArgs);
                    if (output != null && output.Length > 0)
                    {
                        Log.Debug("  output: {Output} (type: {OutputType})", output[0].Value, output[0].Value?.GetType());
                        return Convert.ToDouble(output[0].Value);
                    }
                    Log.Debug("  output: null or empty");
                    return null;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error calling Add method: {Message}", ex.Message);
                    throw;
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var session = await GetSessionAsync(cancellationToken);
            if (session == null)
            {
                Log.Warning("OPC UA session could not be established. Seeding aborted.");
                return;
            }

            // Optionally cache the data if you want to expose it later
            var data = await GetDataAsync(session, cancellationToken);
            // You can store 'data' in a property if needed, e.g. this.SeedInfo = data;
            Log.Information("SeederHostedService started and data fetched.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (_session != null)
            {
                try
                {
                    _session.CloseAsync();
                    _session.Dispose();
                    Log.Information("OPC UA session closed and disposed.");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error closing OPC UA session: {Message}", ex.Message);
                }
                _session = null;
            }
            return Task.CompletedTask;
        }
    }
}
