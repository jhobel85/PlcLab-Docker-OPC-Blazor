using System;
using System.Collections.Generic;
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
            var effectiveSession = await EnsureSessionAsync(session, cancellationToken).ConfigureAwait(false);
            if (effectiveSession == null)
            {
                return new SeedInfo
                {
                    SeedEnabled = false,
                    Variables = new List<SeedVariable>()
                };
            }

            return await _seeder.GetDataAsync(effectiveSession, cancellationToken).ConfigureAwait(false);
        }

        public async Task<double?> InvokeAddAsync(Session? session, float addend1, uint addend2, CancellationToken cancellationToken = default)
        {
            var effectiveSession = await EnsureSessionAsync(session, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Not connected to OPC UA server.");

            return await _seeder.CallMethodAsync<double>(effectiveSession, "Add", addend1, addend2)
                .ConfigureAwait(false);
        }

        private async Task<Session?> EnsureSessionAsync(Session? session, CancellationToken cancellationToken)
        {
            if (session != null && session.Connected)
            {
                return session;
            }

            return await _seeder.GetSessionAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
