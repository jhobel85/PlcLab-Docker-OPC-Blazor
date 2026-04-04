using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Allure.Xunit.Attributes;
using PlcLab.Infrastructure;
using Xunit;

namespace PlcLab.Web.Tests;

/// <summary>
/// Tests for the dark-mode / responsive CSS rules and architecture checks related
/// to the nice-to-have features added in the last sprint.
/// </summary>
[AllureSuite("NiceToHaves")]
public class NiceToHaveTests
{
    // ── CSS — dark mode and responsive rules ──────────────────────────────────

    private static string SiteCssPath =>
        Path.Combine(
            FindRepoRoot(AppContext.BaseDirectory),
            "src", "PlcLab.Web", "wwwroot", "css", "site.css");

    [Fact]
    [AllureFeature("DarkMode CSS")]
    public void SiteCss_Contains_DarkModeMediaQuery()
    {
        var css = File.ReadAllText(SiteCssPath);

        Assert.Contains("prefers-color-scheme: dark", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [AllureFeature("DarkMode CSS")]
    public void SiteCss_DarkMode_DefinesBodyBackground()
    {
        var css = File.ReadAllText(SiteCssPath);

        // Dark background should be defined inside the dark media query block.
        Assert.Contains("background-color", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("#121212", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [AllureFeature("Responsive CSS")]
    public void SiteCss_Contains_MobileBreakpointMediaQuery()
    {
        var css = File.ReadAllText(SiteCssPath);

        Assert.Contains("max-width: 768px", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [AllureFeature("Responsive CSS")]
    public void SiteCss_Responsive_MakesTableScrollable()
    {
        var css = File.ReadAllText(SiteCssPath);

        Assert.Contains("overflow-x: auto", css, StringComparison.OrdinalIgnoreCase);
    }

    // ── IFeatureFlags — architecture checks ───────────────────────────────────

    [Fact]
    [AllureFeature("FeatureFlags arch")]
    public void IFeatureFlags_IsPublicInterface_InInfrastructure()
    {
        var type = typeof(IFeatureFlags);

        Assert.True(type.IsInterface);
        Assert.True(type.IsPublic);
        Assert.Equal("PlcLab.Infrastructure", type.Namespace);
    }

    [Fact]
    [AllureFeature("FeatureFlags arch")]
    public void FeatureFlags_ImplementsIFeatureFlags()
    {
        Assert.True(typeof(IFeatureFlags).IsAssignableFrom(typeof(FeatureFlags)));
    }

    [Fact]
    [AllureFeature("FeatureFlags arch")]
    public void IFeatureFlags_HasSingleIsEnabledMethod()
    {
        var methods = typeof(IFeatureFlags)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName)
            .ToList();

        Assert.Single(methods);
        Assert.Equal("IsEnabled", methods[0].Name);
        Assert.Equal(typeof(bool), methods[0].ReturnType);
    }

    // ── Health check endpoint registration ───────────────────────────────────

    [Fact]
    [AllureFeature("HealthCheck arch")]
    public void AspNetCore_HealthChecks_AssemblyIsReferencedByWebProject()
    {
        // Microsoft.Extensions.Diagnostics.HealthChecks is always present when
        // AddHealthChecks() is called.
        var type = Type.GetType(
            "Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck, Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions",
            throwOnError: false);

        Assert.NotNull(type);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PlcLab-Docker-OPC-Blazor.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from: " + start);
    }
}
