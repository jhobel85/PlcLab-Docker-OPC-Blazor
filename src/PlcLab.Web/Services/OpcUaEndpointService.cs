using System;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace PlcLab.Web.Services
{
    public class OpcUaEndpointService
    {
        private string _endpoint = "opc.tcp://opcua-refserver:50000";
        public event Action? EndpointChanged;
        private readonly IJSRuntime _js;

        public OpcUaEndpointService(IJSRuntime js)
        {
            _js = js;
        }

        public string Endpoint
        {
            get => _endpoint;
            set
            {
                if (_endpoint != value)
                {
                    _endpoint = value;
                    EndpointChanged?.Invoke();
                    _ = SaveToLocalStorage();
                }
            }
        }

        public async Task LoadFromLocalStorage()
        {
            var endpoint = await _js.InvokeAsync<string>("localStorage.getItem", "opcua-endpoint");
            if (!string.IsNullOrWhiteSpace(endpoint))
                _endpoint = endpoint;
        }

        public async Task SaveToLocalStorage()
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "opcua-endpoint", _endpoint);
        }
    }
}
