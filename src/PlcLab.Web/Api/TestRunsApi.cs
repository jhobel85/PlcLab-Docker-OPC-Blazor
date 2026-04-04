using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlcLab.Infrastructure;
using PlcLab.Application;
namespace PlcLab.Web.Api
{
    public static class TestRunsApi
    {
        public static void MapTestRunsApi(WebApplication app)
        {
            // DELETE /api/testruns/{id} - delete a test run and its results
            app.MapDelete("/api/testruns/{id}", async (Guid id, [FromServices] PlcLabDbContext db, CancellationToken cancellationToken) =>
            {
                var run = await db.TestRuns.Include(r => r.Results).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
                if (run == null)
                    return Results.NotFound();
                db.TestRuns.Remove(run);
                await db.SaveChangesAsync(cancellationToken);
                return Results.NoContent();
            })
            .WithTags("Test Runs")
            .WithName("DeleteTestRun")
            .WithSummary("Deletes a test run.")
            .WithDescription("Deletes a test run and any associated test results.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

            app.MapPost("/api/testruns", async (
                [FromBody] TestRunRequest req,
                [FromServices] PlcLabDbContext db,
                [FromServices] IServiceProvider sp,
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
            })
            .WithTags("Test Runs")
            .WithName("CreateTestRun")
            .WithSummary("Executes a test plan and stores a new test run.")
            .WithDescription("Loads a test plan, executes it against the configured OPC UA endpoint, and persists the resulting test run.")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

            // GET /api/testruns - return all test runs with plan name and results
            app.MapGet("/api/testruns", async ([FromServices] PlcLabDbContext db) =>
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
                    Id = r.Id,
                    PlanName = plans.TryGetValue(r.TestPlanId, out var name) ? name : "",
                    StartedAt = r.StartedAt,
                    EndedAt = r.EndedAt,
                    Results = r.Results.Select(res => new TestResultDto { Passed = res.Passed }).ToList()
                }).ToList();
                return Results.Ok(dtos);
            })
            .WithTags("Test Runs")
            .WithName("GetTestRuns")
            .WithSummary("Returns all recorded test runs.")
            .WithDescription("Loads all stored test runs ordered by start time, including plan names and pass/fail summaries.")
            .Produces<List<TestRunDto>>(StatusCodes.Status200OK);
        }
    }
}

// DTOs must be outside the namespace and after all type declarations

public class TestRunDto
{
    public Guid Id { get; set; }
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

