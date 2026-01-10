using Opc.Ua;
using Opc.Ua.Client;
using PlcLab.OPC;
using  PlcLab.Infrastructure;

namespace PlcLab.Web.Services;

public static class SeedInfoApi
{
    public static void MapSeedInfoEndpoint(this WebApplication app)
    {
        app.MapGet("/api/seedinfo", async (IConfiguration config, SeederHostedService seeder) =>
        {
            var seedEnabled = config.GetValue<bool>("Seed:Enabled");
            if (!seedEnabled)
                return Results.Ok(new { seedEnabled, variables = Array.Empty<object>(), debug = "Seeding disabled" });

            // Ensure seeding has run (if needed)
            var session = await seeder.GetSessionAsync(CancellationToken.None);
            var seedInfo = await seeder.GetDataAsync(session, CancellationToken.None);
            var valEnable = seedInfo.Variables.FirstOrDefault(v => v.Label == PlcLabConstants.Enable_Static);
            var boolValue = valEnable?.GetValue<bool>();

            if (seedInfo == null)
                return Results.Ok(new { seedEnabled, variables = Array.Empty<object>(), result = -1, debug = "SeedInfo not available in memory" });
            if (boolValue == false)
                return Results.Ok(new { seedEnabled, variables = Array.Empty<object>(), result = -1, debug = "Add Function not enabled." });

            
            var valFloat = seedInfo.Variables.FirstOrDefault(v => v.Label == PlcLabConstants.Float_Static);
            var valUint = seedInfo.Variables.FirstOrDefault(v => v.Label == PlcLabConstants.Uint_Static);
            var floatValue = valFloat?.GetValue<float>() ?? 0;
            var uintValue = valUint?.GetValue<uint>() ?? 0;
            var ret = await seeder.CallMethodAsync<double>(session, "Add", floatValue, uintValue);

            return Results.Ok(new { seedEnabled = seedInfo.SeedEnabled, variables = seedInfo.Variables, result = ret, debug = "Loaded from SeederHostedService memory" });
        });
    }
}
