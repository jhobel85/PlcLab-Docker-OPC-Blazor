using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Client;
using PlcLab.Domain;

namespace PlcLab.Infrastructure.Services
{
    public interface ISeederService
    {
        Task<Session?> GetSessionAsync(CancellationToken cancellationToken);
        Task<SeedInfo> GetDataAsync(Session session, CancellationToken cancellationToken);
        Task<TResult> CallMethodAsync<TResult>(Session session, string methodName, params object[] args);
    }
}
