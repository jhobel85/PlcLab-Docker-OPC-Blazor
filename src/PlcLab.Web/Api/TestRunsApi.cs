using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using PlcLab.Infrastructure;
using PlcLab.Domain;
using System;
using System.Threading.Tasks;

namespace PlcLab.Web.Api
{
    public static class TestRunsApi
    {
        public static void MapTestRunsApi(WebApplication app)
        {
            app.MapPost("/api/testruns", async (TestRunRequest req, PlcLabDbContext db) =>
            {
                // Validate TestPlan exists
                var plan = await db.TestPlans.Include(p => p.TestCases).FirstOrDefaultAsync(p => p.Id == req.TestPlanId);
                if (plan == null)
                    return Results.NotFound($"TestPlan {req.TestPlanId} not found");

                // Create a new TestRun entity
                var testRun = new TestRun
                {
                    Id = Guid.NewGuid(),
                    TestPlanId = plan.Id,
                    StartedAt = DateTime.UtcNow,
                    EndedAt = null,
                    Results = new()
                };
                // Add a TestResult for each TestCase (simulate pass)
                foreach (var testCase in plan.TestCases)
                {
                    testRun.Results.Add(new TestResult
                    {
                        Id = Guid.NewGuid(),
                        TestCaseId = testCase.Id,
                        Passed = true,
                        Message = "Simulated pass",
                        Timestamp = DateTime.UtcNow
                    });
                }
                db.TestRuns.Add(testRun);
                await db.SaveChangesAsync();

                // Simulate test execution (replace with real logic)
                await Task.Delay(2000); // Simulate work
                testRun.EndedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

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
