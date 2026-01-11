using System.Threading;
using System.Threading.Tasks;
using Opc.Ua.Client;
using PlcLab.Infrastructure;

namespace PlcLab.Web.Services
{
    public interface ISeedDataClient
    {
        Task<SeedInfo> LoadSeedInfoAsync(Session? session, CancellationToken cancellationToken = default);
        Task<double?> InvokeAddAsync(Session? session, float addend1, uint addend2, CancellationToken cancellationToken = default);
    }

    public sealed class SeedDataClient : ISeedDataClient
    {
        private readonly SeederHostedService _seeder;

        public SeedDataClient(SeederHostedService seeder)
        {
            _seeder = seeder;
        }

        public async Task<SeedInfo> LoadSeedInfoAsync(Session? session, CancellationToken cancellationToken = default)
        {
            if (session == null || !session.Connected)
            {
                return new SeedInfo
                {
                    SeedEnabled = false,
                    Variables = new List<SeedVariable>()
                };
            }

            return await _seeder.GetDataAsync(session, cancellationToken).ConfigureAwait(false);
        }

        public async Task<double?> InvokeAddAsync(Session? session, float addend1, uint addend2, CancellationToken cancellationToken = default)
        {
            if (session == null || !session.Connected)
            {
                throw new InvalidOperationException("Not connected to OPC UA server.");
            }

            return await _seeder.CallMethodAsync<double>(session, "Add", addend1, addend2).ConfigureAwait(false);
        }
    }
}
