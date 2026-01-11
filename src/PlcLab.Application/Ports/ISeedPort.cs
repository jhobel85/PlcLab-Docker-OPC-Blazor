using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Client;
using PlcLab.Domain;

namespace PlcLab.Application.Ports
{
    public interface ISeedPort
    {
        Task<SeedInfo> GetSeedInfoAsync(Session session, CancellationToken ct = default);
        Task<double?> InvokeAddAsync(Session session, float addend1, uint addend2, CancellationToken ct = default);
    }
}
