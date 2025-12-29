
using Opc.Ua;
using Opc.Ua.Client;

namespace PlcLab.OPC
{
    public interface IOpcUaClientFactory
    {
        Task<Session> CreateSessionAsync(string discoveryUrl, bool useSecurity = true, CancellationToken ct = default);
    }
}