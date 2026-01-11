using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Client;

namespace PlcLab.Application.Ports
{
    public interface IOpcSessionPort
    {
        Task<Session> CreateSessionAsync(string endpoint, bool useSecurity = true, CancellationToken ct = default);
        string GetApplicationName();
    }
}
