using PlcLab.Application.Ports;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Client;
using Serilog;

namespace PlcLab.Web.Services
{
    public interface IOpcConnectionService
    {
        Session? CurrentSession { get; }
        string Status { get; }
        bool IsConnecting { get; }
        int SessionVersion { get; }
        string ClientApplicationName { get; }
        Task TryConnectAsync(string endpoint, CancellationToken cancellationToken = default);
        Task CloseAsync();
    }

    public sealed class ConnectionStatusService : IOpcConnectionService, IAsyncDisposable
    {
        private static readonly ActivitySource ActivitySource = new("PlcLab.Web.ConnectionStatus");

        private readonly IOpcSessionPort _sessionPort;
        private readonly SemaphoreSlim _connectLock = new(1, 1);

        private Session? _currentSession;
        private string _status = "Not connected";
        private bool _isConnecting;
        private int _sessionVersion;

        public ConnectionStatusService(IOpcSessionPort sessionPort)
        {
            _sessionPort = sessionPort;
        }

        public Session? CurrentSession => _currentSession;
        public string Status => _status;
        public bool IsConnecting => _isConnecting;
        public int SessionVersion => _sessionVersion;
        public string ClientApplicationName => _sessionPort.GetApplicationName();

        public async Task TryConnectAsync(string endpoint, CancellationToken cancellationToken = default)
        {
            await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await ConnectInternalAsync(endpoint, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async Task CloseAsync()
        {
            await CloseCurrentSessionAsync().ConfigureAwait(false);
        }

        private async Task ConnectInternalAsync(string endpoint, CancellationToken cancellationToken)
        {
            _isConnecting = true;
            using var activity = ActivitySource.StartActivity("OpcUaConnect");
            activity?.SetTag("opc.endpoint", endpoint);

            try
            {
                await CloseCurrentSessionAsync().ConfigureAwait(false);

                var session = await _sessionPort.CreateSessionAsync(endpoint, useSecurity: false, cancellationToken)
                    .ConfigureAwait(false);

                _currentSession = session;
                _status = $"Connected (SessionId: {session.SessionId})";
                _sessionVersion++;
                Log.Information("[ConnectionStatusService] Connected to {Endpoint} with SessionId {SessionId}", endpoint, session.SessionId);
            }
            catch (Exception ex)
            {
                activity?.SetTag("error", true);
                activity?.SetTag("exception.message", ex.Message);
                _status = $"Connection failed: {ex.Message}";
                Log.Error(ex, "[ConnectionStatusService] Failed to connect to {Endpoint}", endpoint);
            }
            finally
            {
                _isConnecting = false;
            }
        }

        private async Task CloseCurrentSessionAsync()
        {
            if (_currentSession == null)
            {
                return;
            }

            try
            {
                await _currentSession.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "[ConnectionStatusService] Error closing OPC UA session");
            }
            finally
            {
                _currentSession.Dispose();
                _currentSession = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseCurrentSessionAsync().ConfigureAwait(false);
            _connectLock.Dispose();
        }
    }
}
