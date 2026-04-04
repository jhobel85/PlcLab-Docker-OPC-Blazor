using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Allure.Xunit.Attributes;
using PlcLab.Domain;
using Xunit;

namespace PlcLab.Web.Tests;

[AllureSuite("AuditFields")]
public class AuditFieldsTests
{
    // ── Property existence ────────────────────────────────────────────────────

    [Theory]
    [InlineData("Thumbprint")]
    [InlineData("EndpointUrl")]
    [InlineData("UserIdentity")]
    [AllureFeature("Domain model")]
    public void TestRun_HasAuditProperty(string propertyName)
    {
        var prop = typeof(TestRun).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(prop);
        Assert.True(prop!.CanRead, $"{propertyName} must be readable");
        Assert.True(prop.CanWrite, $"{propertyName} must be writable");
    }

    [Theory]
    [InlineData("Thumbprint")]
    [InlineData("EndpointUrl")]
    [InlineData("UserIdentity")]
    [AllureFeature("Domain model")]
    public void TestRun_AuditProperty_IsNullableString(string propertyName)
    {
        var prop = typeof(TestRun).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;

        Assert.Equal(typeof(string), prop.PropertyType);

        // Verify nullable annotation via NullabilityInfoContext
        var ctx = new NullabilityInfoContext();
        var info = ctx.Create(prop);
        Assert.Equal(NullabilityState.Nullable, info.WriteState);
    }

    // ── Round-trip values ─────────────────────────────────────────────────────

    [Fact]
    [AllureFeature("Round-trip")]
    public void TestRun_AuditFields_CanBeSetAndRead()
    {
        var run = new TestRun
        {
            Id = Guid.NewGuid(),
            TestPlanId = Guid.NewGuid(),
            PlanVersion = 1,
            StartedAt = DateTime.UtcNow,
            Thumbprint = "AABBCCDDEEFF",
            EndpointUrl = "opc.tcp://localhost:4840",
            UserIdentity = "Anonymous"
        };

        Assert.Equal("AABBCCDDEEFF", run.Thumbprint);
        Assert.Equal("opc.tcp://localhost:4840", run.EndpointUrl);
        Assert.Equal("Anonymous", run.UserIdentity);
    }

    [Fact]
    [AllureFeature("Round-trip")]
    public void TestRun_AuditFields_DefaultToNull()
    {
        var run = new TestRun();

        Assert.Null(run.Thumbprint);
        Assert.Null(run.EndpointUrl);
        Assert.Null(run.UserIdentity);
    }

    [Fact]
    [AllureFeature("Round-trip")]
    public void TestRun_AuditFields_AcceptNullAfterSet()
    {
        var run = new TestRun
        {
            Thumbprint = "some-value",
            EndpointUrl = "opc.tcp://host:4840",
            UserIdentity = "user"
        };

        run.Thumbprint = null;
        run.EndpointUrl = null;
        run.UserIdentity = null;

        Assert.Null(run.Thumbprint);
        Assert.Null(run.EndpointUrl);
        Assert.Null(run.UserIdentity);
    }

    // ── EF Core migration file existence ─────────────────────────────────────

    [Fact]
    [AllureFeature("Migration")]
    public void AddAuditFieldsToTestRun_MigrationClass_Exists()
    {
        // Verify that the migration was added to the Infrastructure assembly.
        var infraAssembly = typeof(PlcLab.Infrastructure.PlcLabDbContext).Assembly;

        var migrationTypes = infraAssembly
            .GetTypes()
            .Where(t => t.Namespace == "PlcLab.Infrastructure.Migrations"
                     && t.Name.Contains("AddAuditFieldsToTestRun"))
            .ToList();

        Assert.NotEmpty(migrationTypes);
    }

    // ── TestRun entity still has all original properties ─────────────────────

    [Theory]
    [InlineData("Id", typeof(Guid))]
    [InlineData("TestPlanId", typeof(Guid))]
    [InlineData("PlanVersion", typeof(int))]
    [InlineData("StartedAt", typeof(DateTime))]
    [AllureFeature("Domain model")]
    public void TestRun_OriginalProperties_StillPresent(string name, Type type)
    {
        var prop = typeof(TestRun).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(prop);
        Assert.Equal(type, prop!.PropertyType);
    }
}
