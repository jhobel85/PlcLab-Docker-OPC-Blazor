using PlcLab.Domain;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Opc.Ua.Client;
using PlcLab.Infrastructure;
using PlcLab.Web.Services;

namespace PlcLab.Web.ViewModel
{
    public class IndexViewModel
    {
        private const string DefaultEndpoint = "opc.tcp://opcua-refserver:50000";

        private readonly IOpcConnectionService _connectionService;
        private readonly ISeedDataClient _seedDataClient;
        private readonly IConfiguration _configuration;
        private int _lastObservedSessionVersion = -1;
        private bool _initialized;

        public string OpcStatus => _connectionService.Status;
        public bool IsConnecting => _connectionService.IsConnecting;
        public Session? Session => _connectionService.CurrentSession;
        public string ClientApplicationName => _connectionService.ClientApplicationName;
        public int ClickCount { get; private set; }
        public SeedInfo? SeedInfo { get; private set; }
        public float AddNum1 { get; set; }
        public uint AddNum2 { get; set; }
        public double? AddResult { get; private set; }
        public string AddError { get; private set; } = string.Empty;
        public string SelectedEndpoint { get; set; }

        public int OpcUaSessionKey { get; private set; } = 0;
        public IndexViewModel(
            IOpcConnectionService connectionService,
            ISeedDataClient seedDataClient,
            IConfiguration configuration)
        {
            _connectionService = connectionService;
            _seedDataClient = seedDataClient;
            _configuration = configuration;
            SelectedEndpoint = configuration["OpcUa:Endpoint"] ?? DefaultEndpoint;
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            await TryConnectAsync();
        }

        public async Task CallAddMethodAsync()
        {
            AddResult = null;
            AddError = string.Empty;
            try
            {
                AddResult = await _seedDataClient.InvokeAddAsync(Session, AddNum1, AddNum2);
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
            try
            {
                SeedInfo = await _seedDataClient.LoadSeedInfoAsync(Session);
            }
            catch (Exception ex)
            {
                SeedInfo = new SeedInfo
                {
                    SeedEnabled = false,
                    Variables = new List<SeedVariable>
                    {
                        new SeedVariable
                        {
                            Label = "Error",
                            NodeId = string.Empty,
                            Value = ex.Message
                        }
                    }
                };
            }
            finally
            {
                StatusChanged?.Invoke();
            }
        }

        public event Action? StatusChanged;
        public event Action? Connected;

        public async Task ReconnectAsync()
        {
            await TryConnectAsync();
        }

        public void TestButtonClick()
        {
            ClickCount++;
        }

        public async Task TryConnectAsync()
        {
            await _connectionService.TryConnectAsync(SelectedEndpoint).ConfigureAwait(false);
            await LoadSeedInfoAsync().ConfigureAwait(false);
            ObserveSessionChanges();
        }

        public void ClearSeedInfo()
        {
            SeedInfo = null;
        }

        private void ObserveSessionChanges()
        {
            if (_connectionService.SessionVersion != _lastObservedSessionVersion)
            {
                _lastObservedSessionVersion = _connectionService.SessionVersion;
                if (Session?.Connected == true)
                {
                    OpcUaSessionKey++;
                    Connected?.Invoke();
                }
            }

            StatusChanged?.Invoke();
        }
    }
}
