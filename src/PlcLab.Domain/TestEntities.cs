namespace PlcLab.Domain;

public class TestPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TestCase> TestCases { get; set; } = new();
}

public class TestCase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SignalSnapshot> RequiredSignals { get; set; } = new();
}

public class TestRun
{
    public Guid Id { get; set; }
    public Guid TestPlanId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public List<TestResult> Results { get; set; } = new();
}

public class TestResult
{
    public Guid Id { get; set; }
    public Guid TestCaseId { get; set; }
    public bool Passed { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
    public List<SignalSnapshot> Snapshots { get; set; } = new();
}

public class SignalSnapshot
{
    public Guid Id { get; set; }
    public string SignalName { get; set; } = string.Empty;
    public object? Value { get; set; }
    public DateTime Timestamp { get; set; }
}
