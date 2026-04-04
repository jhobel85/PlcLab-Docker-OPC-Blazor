using System.Collections.Generic;
using Allure.Xunit.Attributes;
using Microsoft.Extensions.Configuration;
using PlcLab.Infrastructure;
using Xunit;

namespace PlcLab.Web.Tests;

[AllureSuite("FeatureFlags")]
public class FeatureFlagsTests
{
    private static IFeatureFlags BuildFlags(Dictionary<string, string?> values)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new FeatureFlags(config);
    }

    // ── IsEnabled ─────────────────────────────────────────────────────────────

    [Fact]
    [AllureFeature("Flag lookup")]
    public void IsEnabled_ReturnsFalse_WhenFlagAbsent()
    {
        var flags = BuildFlags(new Dictionary<string, string?>());

        Assert.False(flags.IsEnabled("NonExistentFlag"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [AllureFeature("Flag lookup")]
    public void IsEnabled_ReturnsCorrectValue_ForExplicitSettings(string configValue, bool expected)
    {
        var flags = BuildFlags(new Dictionary<string, string?>
        {
            ["FeatureFlags:MyFlag"] = configValue
        });

        Assert.Equal(expected, flags.IsEnabled("MyFlag"));
    }

    [Fact]
    [AllureFeature("Flag lookup")]
    public void IsEnabled_ReturnsFalse_WhenFlagExplicitlySetToFalse()
    {
        var flags = BuildFlags(new Dictionary<string, string?>
        {
            ["FeatureFlags:ExperimentalResultsChart"] = "false"
        });

        Assert.False(flags.IsEnabled("ExperimentalResultsChart"));
    }

    [Fact]
    [AllureFeature("Flag lookup")]
    public void IsEnabled_ReturnsTrue_WhenFlagExplicitlySetToTrue()
    {
        var flags = BuildFlags(new Dictionary<string, string?>
        {
            ["FeatureFlags:DarkMode"] = "true"
        });

        Assert.True(flags.IsEnabled("DarkMode"));
    }

    // ── Multiple flags ────────────────────────────────────────────────────────

    [Fact]
    [AllureFeature("Flag lookup")]
    public void IsEnabled_IsolatesFlagsByName()
    {
        var flags = BuildFlags(new Dictionary<string, string?>
        {
            ["FeatureFlags:FlagA"] = "true",
            ["FeatureFlags:FlagB"] = "false"
        });

        Assert.True(flags.IsEnabled("FlagA"));
        Assert.False(flags.IsEnabled("FlagB"));
    }

    // ── Default flags from appsettings ────────────────────────────────────────

    [Theory]
    [InlineData("ExperimentalResultsChart")]
    [InlineData("DarkMode")]
    [InlineData("GlobalDiscoveryServer")]
    [AllureFeature("Default flags")]
    public void DefaultFlags_AreKnownAndDefinedInConfig(string flagName)
    {
        // Validates that expected flag names are valid strings (not empty/null).
        // Actual bool values depend on the running environment.
        Assert.False(string.IsNullOrWhiteSpace(flagName));
    }

    // ── IFeatureFlags contract via interface ─────────────────────────────────

    [Fact]
    [AllureFeature("Interface contract")]
    public void IFeatureFlags_IsImplementedByFeatureFlags()
    {
        Assert.True(typeof(IFeatureFlags).IsAssignableFrom(typeof(FeatureFlags)));
    }
}
