using System;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Client;

namespace PlcLab.Application.Ports
{
    public interface IOpcUaSession : IAsyncDisposable
    {
        Session InnerSession { get; }
    }

    public interface IOpcUaSessionFactory
    {
        Task<IOpcUaSession> CreateSessionAsync(string endpoint, bool useSecurity, CancellationToken ct = default);
    }
}
