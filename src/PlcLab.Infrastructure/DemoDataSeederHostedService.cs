using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace PlcLab.Infrastructure
{
    public class DemoDataSeederHostedService(IConfiguration configuration) : IHostedService
    {
        private readonly IConfiguration _configuration = configuration;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_configuration.GetValue<bool>("Seed:Enabled"))
            {
                Log.Information("Demo data seeding is disabled (Seed:Enabled=false)");
                return Task.CompletedTask;
            }

            Log.Information("Seeding demo OPC UA nodes and methods...");
            // TODO: Connect to OPC UA server and seed demo variables and methods
            // Variables: Process/State, Analog/Flow, Digital/ValveOpen
            // Methods: StartSequenceTest, ResetAlarms
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
