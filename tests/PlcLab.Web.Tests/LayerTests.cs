using NetArchTest.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class LayerTests
{
    private const string Controllers = "MyApp.Controllers";
    private const string Services = "MyApp.Services";
    private const string Repositories = "MyApp.Repositories";

    [Fact]
    /**
    Custom check for cyclic dependencies has been added: the test will go through all types, build a dependency graph between namespaces, and detect cycles using DFS. If it finds a cycle, the test will fail with the message "Cyclic namespace dependencies detected". You can run the tests and verify the result.
    */
    public void No_Cyclic_Dependencies()
    {
        // Build a namespace dependency graph
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !(a.FullName?.StartsWith("System") ?? false) && !(a.FullName?.StartsWith("Microsoft") ?? false));
        var typeNamespaceDeps = new Dictionary<string, HashSet<string>>();

        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (string.IsNullOrEmpty(type.Namespace)) continue;
            var deps = new HashSet<string>();
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (!string.IsNullOrEmpty(field.FieldType.Namespace) && field.FieldType.Namespace != type.Namespace)
                    deps.Add(field.FieldType.Namespace);
            }
            foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (!string.IsNullOrEmpty(prop.PropertyType.Namespace) && prop.PropertyType.Namespace != type.Namespace)
                    deps.Add(prop.PropertyType.Namespace);
            }
            if (!typeNamespaceDeps.ContainsKey(type.Namespace))
                typeNamespaceDeps[type.Namespace] = new HashSet<string>();
            foreach (var dep in deps)
                typeNamespaceDeps[type.Namespace].Add(dep);
        }

        // DFS to detect cycles
        var visited = new HashSet<string>();
        var stack = new HashSet<string>();
        bool HasCycle(string ns)
        {
            if (!visited.Add(ns)) return false;
            stack.Add(ns);
            foreach (var dep in typeNamespaceDeps.GetValueOrDefault(ns) ?? Enumerable.Empty<string>())
            {
                if (stack.Contains(dep) || HasCycle(dep))
                    return true;
            }
            stack.Remove(ns);
            return false;
        }

        bool foundCycle = false;
        foreach (var ns in typeNamespaceDeps.Keys)
        {
            visited.Clear();
            stack.Clear();
            if (HasCycle(ns))
            {
                foundCycle = true;
                break;
            }
        }
        Assert.False(foundCycle, "Cyclic namespace dependencies detected");
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Repositories()
    {
        var result = Types.InNamespace(Controllers)
            .ShouldNot()
            .HaveDependencyOn(Repositories)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Services_Should_Not_Depend_On_Controllers()
    {
        var result = Types.InNamespace(Services)
            .ShouldNot()
            .HaveDependencyOn(Controllers)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Repositories_Should_Not_Depend_On_Controllers_Or_Services()
    {
        // NetArchTest.Rules does not support AndShouldNot in v1.3.2; split into two asserts
        var result1 = Types.InNamespace(Repositories)
            .ShouldNot()
            .HaveDependencyOn(Controllers)
            .GetResult();
        var result2 = Types.InNamespace(Repositories)
            .ShouldNot()
            .HaveDependencyOn(Services)
            .GetResult();

        Assert.True(result1.IsSuccessful);
        Assert.True(result2.IsSuccessful);
    }
}
