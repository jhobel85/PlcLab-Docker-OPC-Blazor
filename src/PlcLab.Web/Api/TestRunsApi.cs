using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlcLab.Infrastructure;
using PlcLab.Domain;
using PlcLab.Application;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlcLab.Web.Api
{
    public static class TestRunsApi
    {
        public static void MapTestRunsApi(WebApplication app)
        {
            app.MapPost("/api/testruns", async (
                TestRunRequest req,
                PlcLabDbContext db,
                IServiceProvider sp,
                CancellationToken cancellationToken) =>
            {
                // Validate TestPlan exists
                var plan = await db.TestPlans.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == req.TestPlanId);
                if (plan == null)
                    return Results.NotFound($"TestPlan {req.TestPlanId} not found");

                var orchestrator = sp.GetRequiredService<TestRunOrchestrator>();
                var config = sp.GetRequiredService<IConfiguration>(); // via appsettings.json
                var opcUaSection = config.GetSection("OpcUa");
                var endpoint = opcUaSection["Endpoint"] ?? opcUaSection["FallbackEndpoint"] ?? "opc.tcp://opcua-refserver:50000";
                var testRun = await orchestrator.ExecuteTestPlanAsync(plan, endpoint, cancellationToken).ConfigureAwait(false);

                // Tag run with plan version for history
                testRun.PlanVersion = plan.Version;

                db.TestRuns.Add(testRun);
                await db.SaveChangesAsync(cancellationToken);

                return Results.Ok(new { testRun.Id, Status = "Completed" });
            });

            // GET /api/testruns - return all test runs with plan name and results
            app.MapGet("/api/testruns", async (PlcLabDbContext db) =>
            {
                var runs = await db.TestRuns
                    .Include(r => r.Results)
                    .OrderByDescending(r => r.StartedAt)
                    .ToListAsync();
                // Fetch all plan names in one go
                var planIds = runs.Select(r => r.TestPlanId).Distinct().ToList();
                var plans = await db.TestPlans
                    .Where(p => planIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Name);
                var dtos = runs.Select(r => new TestRunDto
                {
                    PlanName = plans.TryGetValue(r.TestPlanId, out var name) ? name : "",
                    StartedAt = r.StartedAt,
                    EndedAt = r.EndedAt,
                    Results = r.Results.Select(res => new TestResultDto { Passed = res.Passed }).ToList()
                }).ToList();
                return Results.Ok(dtos);
            });
        }
        public class TestRunDto
        {
            public string PlanName { get; set; } = string.Empty;
            public DateTime StartedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public List<TestResultDto> Results { get; set; } = new();
        }
        public class TestResultDto
        {
            public bool Passed { get; set; }
        }

        public class TestRunRequest
        {
            public Guid TestPlanId { get; set; }
        }
    }
}
