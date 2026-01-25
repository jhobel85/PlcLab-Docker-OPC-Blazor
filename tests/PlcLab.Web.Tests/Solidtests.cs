using Allure.Xunit.Attributes;
using Xunit;
using NetArchTest.Rules;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
namespace PlcLab.Web.Tests;

[AllureSuite("Architecture SOLID Checks")]
public class Solidtests

{
    private static IEnumerable<Assembly> ProjectAssemblies => AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location) && (a.FullName?.StartsWith("PlcLab") ?? false));

    [Fact]
        [AllureFeature("DIP")]
    public void DIP_Services_Should_Depend_On_Interfaces_Not_Implementations()
    {
        var result = Types.InAssemblies(ProjectAssemblies)
            .That().ResideInNamespace("PlcLab.Application.Services")
            .Should()
            .OnlyHaveDependenciesOn("PlcLab.Application.Ports", "System", "PlcLab.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
        [AllureFeature("ISP")]
    public void ISP_Interfaces_Should_Not_Have_Too_Many_Members()
    {
        var interfaces = Types.InAssemblies(ProjectAssemblies)
            .That().AreInterfaces()
            .GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("PlcLab"));
        foreach (var iface in interfaces)
        {
            Assert.True(iface.GetMethods().Length <= 10, $"{iface.Name} has too many members");
        }
    }

    [Fact]
        [AllureFeature("Naming Conventions")]
    public void Controllers_Should_End_With_Controller()
    {
        var result = Types.InAssemblies(ProjectAssemblies)
            .That().ResideInNamespace("PlcLab.Web.Controllers")
            .Should().HaveNameEndingWith("Controller").GetResult();
        Assert.True(result.IsSuccessful);
    }
    [Fact]
        [AllureFeature("Naming Conventions")]
    public void Services_Should_End_With_Service()
    {
        var result = Types.InAssemblies(ProjectAssemblies)
            .That().ResideInNamespace("PlcLab.Application.Services")
            .Should().HaveNameEndingWith("Service").GetResult();
        Assert.True(result.IsSuccessful);
    }
    [Fact]
        [AllureFeature("Naming Conventions")]
    public void Repositories_Should_End_With_Repository()
    {
        var result = Types.InAssemblies(ProjectAssemblies)
            .That().ResideInNamespace("PlcLab.Infrastructure.Repositories")
            .Should().HaveNameEndingWith("Repository").GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
        [AllureFeature("SRP")]
    public void SRP_Classes_Should_Not_Have_Too_Many_Methods()
    {
        var classes = Types.InAssemblies(ProjectAssemblies)
            .That().AreClasses()
            .GetTypes()
            .Where(t => t.Namespace != null 
                && t.Namespace.StartsWith("PlcLab") 
                && t.Name != "IndexViewModel" 
                && t.Name != "PlcLabDbContext"
                && !t.Name.Contains("MockStandardServer")   // Inherits from SDK StandardServer
                && !t.Name.Contains("MockNodeManager"));    // Inherits from SDK CustomNodeManager2
        foreach (var cls in classes)
        {
            var methodCount = cls.GetMethods().Length;
            Assert.True(methodCount <= 20, $"{cls.Name} violates SRP with {methodCount} methods");
        }
    }

    [Fact]
    public void UI_Should_Not_Depend_On_DataAccess()
    {
        var result = Types.InAssemblies(ProjectAssemblies)
            .That().ResideInNamespace("PlcLab.Web.UI")
            .ShouldNot()
            .HaveDependencyOn("PlcLab.Infrastructure.Data")
            .GetResult();
        Assert.True(result.IsSuccessful);
    }
}
