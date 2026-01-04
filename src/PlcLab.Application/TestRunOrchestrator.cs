using PlcLab.Domain;
using PlcLab.OPC;

namespace PlcLab.Application;

public class TestRunOrchestrator
{
    private readonly IOpcUaClientFactory _opcFactory;

    public TestRunOrchestrator(IOpcUaClientFactory opcFactory)
    {
        _opcFactory = opcFactory;
    }

    public async Task<TestRun> ExecuteTestPlanAsync(TestPlan plan, string endpoint, CancellationToken ct = default)
    {
        var run = new TestRun
        {
            Id = Guid.NewGuid(),
            TestPlanId = plan.Id,
            StartedAt = DateTime.UtcNow
        };
        using var session = await _opcFactory.CreateSessionAsync(endpoint, useSecurity: false, ct);
        foreach (var testCase in plan.TestCases)
        {
            var result = await ExecuteTestCaseAsync(session, testCase, ct);
            run.Results.Add(result);
        }
        run.EndedAt = DateTime.UtcNow;
        return run;
    }

    private async Task<TestResult> ExecuteTestCaseAsync(Opc.Ua.Client.Session session, TestCase testCase, CancellationToken ct)
    {
        // Raise event: TestCaseStarted
        var startedEvent = new TestCaseStarted(testCase.Id);
        // ...event dispatch logic here (if needed)...

        // Simulate test logic (read signals, check values, etc.)
        bool passed = true;
        string? message = null;
        var snapshots = new List<SignalSnapshot>();
        foreach (var signal in testCase.RequiredSignals)
        {
            // Example: read value from OPC UA
            try
            {
                var value = await _opcFactory.ReadValueAsync(session, new Opc.Ua.NodeId(signal.SignalName), ct);
                snapshots.Add(new SignalSnapshot
                {
                    Id = Guid.NewGuid(),
                    SignalName = signal.SignalName,
                    Value = value,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                passed = false;
                message = $"Signal {signal.SignalName} read failed: {ex.Message}";
                break;
            }
        }
        // Raise event: TestCasePassed/Failed
        if (passed)
        {
            var passedEvent = new TestCasePassed(testCase.Id);
            // ...event dispatch logic here...
        }
        else
        {
            var failedEvent = new TestCaseFailed(testCase.Id, message ?? "Unknown error");
            // ...event dispatch logic here...
        }
        return new TestResult
        {
            Id = Guid.NewGuid(),
            TestCaseId = testCase.Id,
            Passed = passed,
            Message = message,
            Timestamp = DateTime.UtcNow,
            Snapshots = snapshots
        };
    }
}
