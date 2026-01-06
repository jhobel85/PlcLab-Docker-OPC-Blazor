using Microsoft.AspNetCore.Components;
using Opc.Ua.Client;
using PlcLab.OPC;
using PlcLab.Infrastructure;
using System.Diagnostics;

namespace PlcLab.Web.ViewModel
{
    public class IndexViewModel
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource("PlcLab.Web.IndexViewModel");
        public IOpcUaClientFactory UaFactory { get; }
        public IConfiguration Configuration { get; }
        public SeederHostedService SeedService { get; }
        public NavigationManager NavigationManager { get; }
        public string OpcStatus { get; private set; } = "Not connected";
        public bool IsConnecting { get; private set; }
        public Session? Session { get; private set; }
        public int ClickCount { get; private set; }
        public SeedInfo? SeedInfo { get; private set; }
        public float AddNum1 { get; set; }
        public uint AddNum2 { get; set; }
        public double? AddResult { get; private set; }
        public string AddError { get; private set; } = string.Empty;
        public string SelectedEndpoint { get; set; }

        public IndexViewModel(IOpcUaClientFactory uaFactory, IConfiguration configuration, SeederHostedService seedService, NavigationManager navigationManager)
        {
            UaFactory = uaFactory;
            Configuration = configuration;
            SeedService = seedService;
            NavigationManager = navigationManager;
            // Default to config value or Virtual PLC
            SelectedEndpoint = configuration["OpcUa:Endpoint"] ?? "opc.tcp://opcua-refserver:50000";
        }

        public async Task InitializeAsync()
        {
            await TryConnectAsync();
            await LoadSeedInfoAsync();
        }

        public async Task CallAddMethodAsync()
        {
            AddResult = null;
            AddError = string.Empty;
            try
            {
                AddResult = await SeedService.CallMethodAsync<double>(Session, "Add", AddNum1, AddNum2);
                if (AddResult == null)
                    AddError = "No result returned.";
            }
            catch (Exception ex)
            {
                AddError = ex.Message;
            }
        }

        public async Task LoadSeedInfoAsync()
        {
            var variables = new List<SeedVariable>();
            bool seedEnabled = true;
            if (Session == null || !Session.Connected)
            {
                SeedInfo = new SeedInfo { SeedEnabled = false, Variables = new List<SeedVariable>() };
                return;
            }
            try
            {
                var nodeIds = SeedDemoData.Variables.Select(v => new Opc.Ua.NodeId(v.NodeId)).ToList();
                Opc.Ua.DataValue[]? values = null;
                if (nodeIds.Count > 0)
                {
                    var readResult = await Session.ReadValuesAsync(nodeIds);
                    values = readResult.Item1?.ToArray();
                }
                for (int i = 0; i < SeedDemoData.Variables.Length; i++)
                {
                    var label = SeedDemoData.Variables[i].Label;
                    var nodeId = nodeIds[i].ToString();
                    var value = values != null && i < values.Length ? values[i].Value : null;
                    variables.Add(new SeedVariable { Label = label, NodeId = nodeId, Value = value?.ToString() ?? string.Empty });
                }
            }
            catch (Exception ex)
            {
                variables.Add(new SeedVariable { Label = "Error", NodeId = "", Value = $"Error: {ex.Message}" });
            }
            SeedInfo = new SeedInfo { SeedEnabled = seedEnabled, Variables = variables };
        }

        public event Action? StatusChanged;
        public event Action? Connected;

        public async Task ReconnectAsync()
        {
            // Do not clear OpcStatus here; let TryConnectAsync update it after result is known
            await TryConnectAsync();
            StatusChanged?.Invoke(); // Notify UI to update after reconnect
        }

        public void TestButtonClick()
        {
            ClickCount++;
        }

        public async Task TryConnectAsync()
        {
            var endpoint = SelectedEndpoint;
            IsConnecting = true;
            StatusChanged?.Invoke();
            using var activity = ActivitySource.StartActivity("OpcUaConnect");
            activity?.SetTag("opc.endpoint", endpoint);
            bool connectFailed = false;
            string? connectError = null;
            try
            {
                if (Session != null)
                {
                    try
                    {
                        await Session.CloseAsync();
                    }
                    catch { }
                    finally
                    {
                        Session.Dispose();
                        Session = null;
                    }
                }
                await Task.Delay(1000);
                Session = await UaFactory.CreateSessionAsync(endpoint, useSecurity: false);
            }
            catch (Exception ex)
            {
                connectFailed = true;
                connectError = ex.Message;
                activity?.SetTag("error", true);
                activity?.SetTag("exception.message", ex.Message);
            }
            finally
            {
                var sessionId = Session?.SessionId?.ToString() ?? "(unknown)";
                if (Session != null && Session.Connected)
                {
                    OpcStatus = $"Connected (SessionId: {sessionId})";
                    Connected?.Invoke();
                }
                else if (connectFailed)
                {
                    OpcStatus = $"Connection failed: {connectError}";
                }
                else
                {
                    OpcStatus = "Not connected";
                }
                IsConnecting = false;
                StatusChanged?.Invoke();
            }
        }

        public void ClearSeedInfo()
        {
            SeedInfo = null;
        }
    }
}
