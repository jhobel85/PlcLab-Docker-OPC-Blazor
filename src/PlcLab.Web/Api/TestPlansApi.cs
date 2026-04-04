using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlcLab.Domain;
using PlcLab.Infrastructure;

namespace PlcLab.Web.Api;

public static class TestPlansApi
{
    public static void MapTestPlansApi(this WebApplication app)
    {
        app.MapGet("/api/testplans", async ([FromServices] PlcLabDbContext db) =>
        {
            var plans = await db.TestPlans
                .Include(tp => tp.TestCases)
                .ToListAsync();
            return Results.Ok(plans);
        })
        .WithTags("Test Plans")
        .WithName("GetTestPlans")
        .WithSummary("Returns all test plans.")
        .WithDescription("Loads all test plans including their test cases.")
        .Produces<List<TestPlan>>(StatusCodes.Status200OK);

        app.MapPost("/api/testplans", async ([FromServices] PlcLabDbContext db, [FromBody] TestPlan plan) =>
        {
            plan.Id = Guid.NewGuid();
            if (plan.Version <= 0) plan.Version = 1;
            foreach (var tc in plan.TestCases)
                tc.Id = Guid.NewGuid();
            db.TestPlans.Add(plan);
            await db.SaveChangesAsync();
            return Results.Created($"/api/testplans/{plan.Id}", plan);
        })
        .WithTags("Test Plans")
        .WithName("CreateTestPlan")
        .WithSummary("Creates a new test plan.")
        .WithDescription("Creates a new test plan and assigns identifiers to the plan and its test cases.")
        .Produces<TestPlan>(StatusCodes.Status201Created);

        app.MapGet("/api/testplans/{id:guid}", async ([FromServices] PlcLabDbContext db, Guid id) =>
        {
            var plan = await db.TestPlans
                .Include(tp => tp.TestCases)
                .FirstOrDefaultAsync(tp => tp.Id == id);
            return plan is null ? Results.NotFound() : Results.Ok(plan);
        })
        .WithTags("Test Plans")
        .WithName("GetTestPlanById")
        .WithSummary("Returns a single test plan by identifier.")
        .WithDescription("Loads a test plan and its test cases by plan identifier.")
        .Produces<TestPlan>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapPut("/api/testplans/{id:guid}", async ([FromServices] PlcLabDbContext db, Guid id, [FromBody] TestPlan plan) =>
        {
            // Check if plan exists
            var exists = await db.TestPlans.AnyAsync(tp => tp.Id == id);
            if (!exists) return Results.NotFound();

            // Delete old test cases directly in DB (bypasses change tracker)
            await db.TestCases.Where(tc => tc.TestPlanId == id).ExecuteDeleteAsync();

            // Update plan fields directly in DB (bypasses change tracker)
            await db.TestPlans
                .Where(tp => tp.Id == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(tp => tp.Name, plan.Name)
                    .SetProperty(tp => tp.Version, tp => tp.Version + 1));

            // Add new test cases directly
            var newCases = plan.TestCases.Select(tc => new TestCase
            {
                Id = Guid.NewGuid(),
                Name = tc.Name,
                Description = tc.Description,
                RequiredSignals = tc.RequiredSignals,
                TestPlanId = id
            }).ToList();
            db.TestCases.AddRange(newCases);
            await db.SaveChangesAsync();

            // Re-fetch to return the updated plan with all test cases
            var updated = await db.TestPlans.AsNoTracking().Include(tp => tp.TestCases).FirstOrDefaultAsync(tp => tp.Id == id);
            return Results.Ok(updated);
        })
        .WithTags("Test Plans")
        .WithName("UpdateTestPlan")
        .WithSummary("Updates an existing test plan.")
        .WithDescription("Replaces the test cases of an existing plan and increments the plan version.")
        .Produces<TestPlan>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        app.MapDelete("/api/testplans/{id:guid}", async ([FromServices] PlcLabDbContext db, Guid id) =>
        {
            var plan = await db.TestPlans.Include(tp => tp.TestCases).FirstOrDefaultAsync(tp => tp.Id == id);
            if (plan == null) return Results.NotFound();
            db.TestPlans.Remove(plan);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithTags("Test Plans")
        .WithName("DeleteTestPlan")
        .WithSummary("Deletes a test plan.")
        .WithDescription("Deletes a test plan and its associated test cases.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
