using Xunit;
using NetArchTest.Rules;
using System.Linq;

public class Solidtests
{
    [Fact]
    public void DIP_Services_Should_Depend_On_Interfaces_Not_Implementations()
    {
        var result = Types.InNamespace("PlcLab.Application.Services")
            .Should()
            .OnlyHaveDependenciesOn("PlcLab.Application.Ports", "System", "PlcLab.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void ISP_Interfaces_Should_Not_Have_Too_Many_Members()
    {
        var interfaces = Types.InCurrentDomain()
            .That().AreInterfaces()
            .GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("PlcLab"));
        foreach (var iface in interfaces)
        {
            Assert.True(iface.GetMethods().Length <= 10, $"{iface.Name} has too many members");
        }
    }

    [Fact]
    public void Controllers_Should_End_With_Controller()
    {
        var result = Types.InNamespace("PlcLab.Web.Controllers").Should().HaveNameEndingWith("Controller").GetResult();
        Assert.True(result.IsSuccessful);
    }
    [Fact]
    public void Services_Should_End_With_Service()
    {
        var result = Types.InNamespace("PlcLab.Application.Services").Should().HaveNameEndingWith("Service").GetResult();
        Assert.True(result.IsSuccessful);
    }
    [Fact]
    public void Repositories_Should_End_With_Repository()
    {
        var result = Types.InNamespace("PlcLab.Infrastructure.Repositories").Should().HaveNameEndingWith("Repository").GetResult();
        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void SRP_Classes_Should_Not_Have_Too_Many_Methods()
    {
        var classes = Types.InCurrentDomain()
            .That().AreClasses()
            .GetTypes()
            .Where(t => t.Namespace != null && t.Namespace.StartsWith("PlcLab") && t.Name != "IndexViewModel" && t.Name != "PlcLabDbContext");
        foreach (var cls in classes)
        {
            var methodCount = cls.GetMethods().Length;
            Assert.True(methodCount <= 20, $"{cls.Name} violates SRP with {methodCount} methods");
        }
    }

    [Fact]
    public void UI_Should_Not_Depend_On_DataAccess()
    {
        var result = Types.InNamespace("PlcLab.Web.UI")
            .ShouldNot()
            .HaveDependencyOn("PlcLab.Infrastructure.Data")
            .GetResult();
        Assert.True(result.IsSuccessful);
    }
}
