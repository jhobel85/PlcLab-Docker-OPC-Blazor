using System;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Client;
using PlcLab.Application.Ports;

public class OpcUaSessionFactoryImpl : IOpcUaSessionFactory
{
    private readonly IOpcSessionPort _sessionPort;
    public OpcUaSessionFactoryImpl(IOpcSessionPort sessionPort) => _sessionPort = sessionPort;
    public async Task<IOpcUaSession> CreateSessionAsync(string endpoint, bool useSecurity, CancellationToken ct = default)
    {
        var session = await _sessionPort.CreateSessionAsync(endpoint, useSecurity, ct);
        return new OpcUaSessionWrapper(session);
    }
    private sealed class OpcUaSessionWrapper : IOpcUaSession
    {
        public OpcUaSessionWrapper(Session session) => InnerSession = session;
        public Session InnerSession { get; }
        public ValueTask DisposeAsync() { InnerSession.Dispose(); return ValueTask.CompletedTask; }
    }
}
