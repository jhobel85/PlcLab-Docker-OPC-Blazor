using System;
using System.Threading.Tasks;
using Opc.Ua;
using PlcLab.OPC;
using PlcLab.OPC.Adapters;
using Xunit;
using Xunit.Abstractions;

namespace PlcLab.Web.Tests.Integration;

/// <summary>
/// Integration tests against the OPC UA Reference Server running in Docker.
/// These tests require the Docker Compose stack to be running: docker compose up opcua-refserver
/// </summary>
public class ReferenceServerIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private const string ReferenceServerEndpoint = "opc.tcp://localhost:4840";

    public ReferenceServerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Run in CI with Docker Compose - skip locally to avoid port conflicts")]
    public async Task CanConnectToReferenceServer()
    {
        // Arrange
        var telemetry = SerilogTelemetry.Create();
        var sessionAdapter = new OpcSessionAdapter(telemetry);

        // Act
        using var session = await sessionAdapter.CreateSessionAsync(
            ReferenceServerEndpoint,
            useSecurity: false
        );

        // Assert
        Assert.NotNull(session);
        Assert.True(session.Connected);
        _output.WriteLine($"Successfully connected to OPC UA Reference Server");
        _output.WriteLine($"Endpoint: {session?.Endpoint?.EndpointUrl ?? "unknown"}");
        _output.WriteLine($"Session Name: {session?.SessionName ?? "unknown"}");
    }
}
