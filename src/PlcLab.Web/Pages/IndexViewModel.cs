using Microsoft.AspNetCore.Components;
using Opc.Ua.Client;
using PlcLab.OPC;
using PlcLab.Web.Models;
using PlcLab.Infrastructure;

namespace PlcLab.Web.Pages
{
    public class IndexViewModel
    {
        public IOpcUaClientFactory UaFactory { get; }
        public IConfiguration Configuration { get; }
        public DemoDataSeederHostedService AddService { get; }
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

        public IndexViewModel(IOpcUaClientFactory uaFactory, IConfiguration configuration, DemoDataSeederHostedService addService, NavigationManager navigationManager)
        {
            UaFactory = uaFactory;
            Configuration = configuration;
            AddService = addService;
            NavigationManager = navigationManager;
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
                AddResult = await AddService.CallAddMethodAsync(Session, AddNum1, AddNum2);
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
                var http = new HttpClient { BaseAddress = new Uri(NavigationManager.BaseUri) };
                var resp = await http.GetAsync("api/seedinfo");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    SeedInfo = System.Text.Json.JsonSerializer.Deserialize<SeedInfo>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load seed info: {ex.Message}");
            }
        }

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
            var endpoint = Configuration["OpcUa:Endpoint"] ?? "opc.tcp://localhost:4840";
            IsConnecting = true;
            OpcStatus = $"Connecting to {endpoint}...";
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
                var sessionId = Session?.SessionId?.ToString() ?? "(unknown)";
                OpcStatus = $"Connected (SessionId: {sessionId})";
            }
            catch (Exception ex)
            {
                OpcStatus = $"Connection failed: {ex.Message}";
            }
            finally
            {
                IsConnecting = false;
            }
        }
    }
}
