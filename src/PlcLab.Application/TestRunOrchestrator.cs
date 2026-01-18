using PlcLab.Domain;

namespace PlcLab.Application;

using PlcLab.Application.Ports;
using Opc.Ua.Client;

public class TestRunOrchestrator
{
    private readonly IOpcUaSessionFactory _sessionFactory;
    private readonly Ports.IReadWritePort _readWritePort;

    public TestRunOrchestrator(IOpcUaSessionFactory sessionFactory, Ports.IReadWritePort readWritePort)
    {
        _sessionFactory = sessionFactory;
        _readWritePort = readWritePort;
    }

    public async Task<TestRun> ExecuteTestPlanAsync(TestPlan plan, string endpoint, CancellationToken ct = default)
    {
        var run = new TestRun
        {
            Id = Guid.NewGuid(),
            TestPlanId = plan.Id,
            StartedAt = DateTime.UtcNow
        };
        await using var session = await _sessionFactory.CreateSessionAsync(endpoint, useSecurity: false, ct);
        foreach (var testCase in plan.TestCases)
        {
            var result = await ExecuteTestCaseAsync(session, testCase, ct);
            run.Results.Add(result);
        }
        run.EndedAt = DateTime.UtcNow;
        return run;
    }

    private async Task<TestResult> ExecuteTestCaseAsync(IOpcUaSession session, TestCase testCase, CancellationToken ct)
    {
        // Raise event: TestCaseStarted
        var startedEvent = new TestCaseStarted(testCase.Id);
        // ...event dispatch logic here (if needed)...

        // Simulate test logic (read signals, check values, etc.)
        bool passed = true;
        string? message = null;
        var snapshots = new List<SignalSnapshot>();
        var signals = testCase.RequiredSignals ?? new List<SignalSnapshot>();
        foreach (var signal in signals)
        {
            // Example: read value from OPC UA
            try
            {
                var value = await _readWritePort.ReadValueAsync(session.InnerSession, new Opc.Ua.NodeId(signal.SignalName), ct);
                Console.WriteLine($"ReadValueAsync for {signal.SignalName} returned: {value}");
                snapshots.Add(new SignalSnapshot
                {
                    Id = Guid.NewGuid(),
                    SignalName = signal.SignalName,
                    Value = value?.ToString(),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                passed = false;
                message = $"Signal {signal.SignalName} read failed: {ex.Message}";
                Console.WriteLine($"Exception in ReadValueAsync: {ex}");
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
