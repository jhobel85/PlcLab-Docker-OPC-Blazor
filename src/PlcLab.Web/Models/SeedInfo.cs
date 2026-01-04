namespace PlcLab.Web.Models;

public class SeedInfo
{
    public bool SeedEnabled { get; set; }
    public List<SeedVariable> Variables { get; set; } = [];
}

public class SeedVariable
{
    public string Label { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    // Example method: parse value as double (if possible)
    public double? GetValueAsDouble()
    {
        if (double.TryParse(Value, out var d))
            return d;
        return null;
    }
}
