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
        .RequireAuthorization();

        app.MapPost("/api/testplans", async ([FromServices] PlcLabDbContext db, [FromBody] TestPlan plan) =>
        {
            plan.Id = Guid.NewGuid();
            foreach (var tc in plan.TestCases)
                tc.Id = Guid.NewGuid();
            db.TestPlans.Add(plan);
            await db.SaveChangesAsync();
            return Results.Created($"/api/testplans/{plan.Id}", plan);
        })
        .RequireAuthorization();

        app.MapPut("/api/testplans/{id:guid}", async ([FromServices] PlcLabDbContext db, Guid id, [FromBody] TestPlan plan) =>
        {
            var existing = await db.TestPlans.Include(tp => tp.TestCases).FirstOrDefaultAsync(tp => tp.Id == id);
            if (existing == null) return Results.NotFound();
            existing.Name = plan.Name;
            // Replace cases
            db.TestCases.RemoveRange(existing.TestCases);
            foreach (var tc in plan.TestCases)
                tc.Id = Guid.NewGuid();
            existing.TestCases = plan.TestCases;
            await db.SaveChangesAsync();
            return Results.Ok(existing);
        })
        .RequireAuthorization();

        app.MapDelete("/api/testplans/{id:guid}", async ([FromServices] PlcLabDbContext db, Guid id) =>
        {
            var plan = await db.TestPlans.Include(tp => tp.TestCases).FirstOrDefaultAsync(tp => tp.Id == id);
            if (plan == null) return Results.NotFound();
            db.TestPlans.Remove(plan);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
