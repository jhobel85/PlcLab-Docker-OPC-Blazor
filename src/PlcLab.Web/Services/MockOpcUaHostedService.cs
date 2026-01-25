using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlcLab.OPC.Mock;

namespace PlcLab.Web.Services;

/// <summary>
/// Starts the in-process mock OPC UA server for local/demo scenarios.
/// </summary>
public sealed class MockOpcUaHostedService : IHostedService
{
    private readonly MockOpcUaServer _server;
    private readonly MockOpcUaOptions _options;
    private readonly ILogger<MockOpcUaHostedService> _logger;

    public MockOpcUaHostedService(MockOpcUaServer server, IOptions<MockOpcUaOptions> options, ILogger<MockOpcUaHostedService> logger)
    {
        _server = server;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Mock OPC UA server disabled by config.");
            return;
        }

        _logger.LogInformation("Starting mock OPC UA server at {Endpoint}", _server.EndpointUrl);
        await _server.StartAsync(cancellationToken);
        _logger.LogInformation("Mock OPC UA server started at {Endpoint}", _server.EndpointUrl);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        _logger.LogInformation("Stopping mock OPC UA server...");
        await _server.DisposeAsync();
        _logger.LogInformation("Mock OPC UA server stopped.");
    }
}
