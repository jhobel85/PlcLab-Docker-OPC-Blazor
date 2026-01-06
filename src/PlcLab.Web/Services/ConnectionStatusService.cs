using Opc.Ua.Client;
using System;
using Serilog;

namespace PlcLab.Web.Services
{
    public class ConnectionStatusService
    {
        private Session? _currentSession;
        private string _status = "Not connected";
        
        public event Action? StatusChanged;
        public event Func<Task>? ReconnectRequested;

        public async Task RequestReconnectAsync()
        {
            Log.Information("[ConnectionStatusService] RequestReconnectAsync called");
            if (ReconnectRequested != null)
            {
                Log.Information($"[ConnectionStatusService] Invoking {ReconnectRequested.GetInvocationList().Length} handler(s)");
                foreach (var handler in ReconnectRequested.GetInvocationList())
                {
                    await ((Func<Task>)handler)();
                }
            }
            else
            {
                Log.Information("[ConnectionStatusService] No ReconnectRequested handlers subscribed!");
            }
        }

        public Session? CurrentSession
        {
            get => _currentSession;
            set
            {
                if (_currentSession != value)
                {
                    _currentSession = value;
                    OnStatusChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnStatusChanged();
                }
            }
        }

        private void OnStatusChanged()
        {
            StatusChanged?.Invoke();
        }
    }
}
