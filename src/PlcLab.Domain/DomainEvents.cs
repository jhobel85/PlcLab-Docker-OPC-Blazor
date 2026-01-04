namespace PlcLab.Domain;

public abstract class DomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class TestCaseStarted : DomainEvent
{
    public Guid TestCaseId { get; }
    public TestCaseStarted(Guid testCaseId) => TestCaseId = testCaseId;
}

public class TestCasePassed : DomainEvent
{
    public Guid TestCaseId { get; }
    public TestCasePassed(Guid testCaseId) => TestCaseId = testCaseId;
}

public class TestCaseFailed : DomainEvent
{
    public Guid TestCaseId { get; }
    public string Reason { get; }
    public TestCaseFailed(Guid testCaseId, string reason)
    {
        TestCaseId = testCaseId;
        Reason = reason;
    }
}
