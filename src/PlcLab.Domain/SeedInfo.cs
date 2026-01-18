namespace PlcLab.Domain;

public class SeedInfo
{
    public bool SeedEnabled { get; set; }
    public List<SeedVariable> Variables { get; set; } = [];
}

public class SeedVariable
{
    public string Label { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public object? Value { get; set; }

    //stores the name of the original .NET type of the value
    public string ValueType { get; set; } = string.Empty;

    public T? GetValue<T>()
    {
        if (Value is T t)
            return t;
        if (Value is null)
            return default;
        try
        {
            return (T)Convert.ChangeType(Value, typeof(T));
        }
        catch
        {
            return default;
        }
    }
}
