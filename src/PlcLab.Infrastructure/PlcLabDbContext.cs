using Microsoft.EntityFrameworkCore;
using PlcLab.Domain;

namespace PlcLab.Infrastructure;

public class PlcLabDbContext : DbContext
{
    public PlcLabDbContext(DbContextOptions<PlcLabDbContext> options) : base(options) { }

    public DbSet<TestPlan> TestPlans => Set<TestPlan>();
    public DbSet<TestCase> TestCases => Set<TestCase>();
    public DbSet<TestRun> TestRuns => Set<TestRun>();
    public DbSet<TestResult> TestResults => Set<TestResult>();
    public DbSet<SignalSnapshot> SignalSnapshots => Set<SignalSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships and keys as needed
        modelBuilder.Entity<TestPlan>()
            .HasMany(tp => tp.TestCases)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TestRun>()
            .HasMany(tr => tr.Results)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TestResult>()
            .HasMany(tr => tr.Snapshots)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
