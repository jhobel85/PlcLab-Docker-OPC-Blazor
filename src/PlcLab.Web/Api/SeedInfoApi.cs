using PlcLab.Domain;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PlcLab.Infrastructure;
using PlcLab.Web.Services;

namespace PlcLab.Web.Services;

public static class SeedInfoApi
{
    public static void MapSeedInfoEndpoint(this WebApplication app)
    {
        app.MapGet("/api/seedinfo", async (
            IConfiguration config,
            ISeedDataClient seedDataClient,
            CancellationToken cancellationToken) =>
        {
            var seedEnabled = config.GetValue<bool>("Seed:Enabled");
            if (!seedEnabled)
            {
                return Results.Ok(new { seedEnabled, variables = Array.Empty<object>(), result = (double?)null, debug = "Seeding disabled" });
            }

            var seedInfo = await seedDataClient.LoadSeedInfoAsync(null, cancellationToken).ConfigureAwait(false);
            if (!seedInfo.SeedEnabled)
            {
                return Results.Ok(new { seedEnabled = false, variables = Array.Empty<object>(), result = (double?)null, debug = "Seed info unavailable" });
            }

            var valEnable = seedInfo.Variables.FirstOrDefault(v => v.Label == PlcLabConstants.Enable_Static);
            var boolValue = valEnable?.GetValue<bool>();
            if (boolValue == false)
            {
                return Results.Ok(new { seedEnabled = seedInfo.SeedEnabled, variables = seedInfo.Variables, result = (double?)null, debug = "Add Function not enabled." });
            }

            var valFloat = seedInfo.Variables.FirstOrDefault(v => v.Label == PlcLabConstants.Float_Static);
            var valUint = seedInfo.Variables.FirstOrDefault(v => v.Label == PlcLabConstants.Uint_Static);
            var floatValue = valFloat?.GetValue<float>() ?? 0;
            var uintValue = valUint?.GetValue<uint>() ?? 0;
            var result = await seedDataClient.InvokeAddAsync(null, floatValue, uintValue, cancellationToken).ConfigureAwait(false);

            return Results.Ok(new { seedEnabled = seedInfo.SeedEnabled, variables = seedInfo.Variables, result, debug = "Loaded via SeedDataClient" });
        })
        .RequireAuthorization();
    }
}
