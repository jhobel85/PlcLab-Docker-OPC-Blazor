using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlcLab.Web.Tests;

// Lightweight placeholder simulator used with mocked read/write.
public sealed class OpcUaSim
{
    private readonly Dictionary<string, object?> _values = new();

    public OpcUaSim(Dictionary<string, object?> seed)
    {
        foreach (var kvp in seed)
            _values[kvp.Key] = kvp.Value;
    }

    public string EndpointUrl { get; private set; } = "opc.tcp://localhost:4841/OpcUaSim";

    public Task StartAsync() => Task.CompletedTask;

    public object? GetValue(string nodeId) => _values.TryGetValue(nodeId, out var v) ? v : null;
}

