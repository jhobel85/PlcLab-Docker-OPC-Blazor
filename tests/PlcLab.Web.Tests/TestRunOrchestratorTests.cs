using Allure.Xunit.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PlcLab.Application;
using PlcLab.Application.Ports;
using PlcLab.Domain;
using Xunit;
using Opc.Ua;

namespace PlcLab.Web.Tests
{
    [AllureSuite("TestRunOrchestrator")]
    public class TestRunOrchestratorTests        
    {
        public static IEnumerable<object?[]> ExecutePlanScenarios() => new List<object?[]>
        {
            new object?[] { "success", false, false, true, null, 1 },
            new object?[] { "read fails", true, false, false, "Signal ns=2;s=Signal1 read failed", 0 },
            new object?[] { "null signals", false, true, true, null, 0 }
        };

        [Theory]
        [MemberData(nameof(ExecutePlanScenarios))]
        [AllureFeature("Test Plan Execution")]
        public async Task ExecuteTestPlanAsync_Scenarios(string _scenario, bool throwsOnRead, bool signalsNull, bool expectedPassed, string? expectedMessageContains, int expectedSnapshots)
        {
            // Use scenario name for test output or debugging
            System.Diagnostics.Debug.WriteLine($"Running scenario: {_scenario}");
#pragma warning disable SYSLIB0050
            var realSession = (Opc.Ua.Client.Session)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Opc.Ua.Client.Session));
#pragma warning restore SYSLIB0050
            var opcUaSessionStub = new TestOpcUaSession(realSession);

            var sessionFactoryMock = new Mock<IOpcUaSessionFactory>();
            sessionFactoryMock.Setup(f => f.CreateSessionAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(opcUaSessionStub);

            var readWritePortMock = new Mock<IReadWritePort>();
            if (throwsOnRead)
            {
                readWritePortMock.Setup(p => p.ReadValueAsync(realSession, It.IsAny<Opc.Ua.NodeId>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("boom"));
            }
            else
            {
                readWritePortMock.Setup(p => p.ReadValueAsync(realSession, It.IsAny<Opc.Ua.NodeId>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("42");
            }

            var orchestrator = new TestRunOrchestrator(sessionFactoryMock.Object, readWritePortMock.Object);
            var testPlan = new TestPlan
            {
                Id = Guid.NewGuid(),
                TestCases =
                [
                    new()
                    {
                        Id = Guid.NewGuid(),
                        RequiredSignals = signalsNull
                            ? null!
                            : [new() { SignalName = "ns=2;s=Signal1" }]
                    }
                ]
            };

            // Act
            var result = await orchestrator.ExecuteTestPlanAsync(testPlan, "opc.tcp://test", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testPlan.Id, result.TestPlanId);
            Assert.Single(result.Results);

            var caseResult = result.Results[0];
            Assert.Equal(expectedPassed, caseResult.Passed);
            Assert.Equal(expectedSnapshots, caseResult.Snapshots.Count);

            if (expectedMessageContains == null)
            {
                Assert.Null(caseResult.Message);
            }
            else
            {
                Assert.NotNull(caseResult.Message);
                Assert.Contains(expectedMessageContains, caseResult.Message);
            }
        }

        [Fact]
        [AllureFeature("OPC UA Simulation")]
        public async Task ExecuteTestPlanAsync_UsesSimPlaceholder()
        {
            var sim = new OpcUaSim(new Dictionary<string, object?> { { "ns=2;s=SimulatedSignal", "99" } });
            await sim.StartAsync();

#pragma warning disable SYSLIB0050
            var realSession = (Opc.Ua.Client.Session)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Opc.Ua.Client.Session));
#pragma warning restore SYSLIB0050
            var opcUaSessionStub = new TestOpcUaSession(realSession);

            var sessionFactoryMock = new Mock<IOpcUaSessionFactory>();
            sessionFactoryMock.Setup(f => f.CreateSessionAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(opcUaSessionStub);

            var readWritePortMock = new Mock<IReadWritePort>();
            readWritePortMock.Setup(p => p.ReadValueAsync(realSession, It.IsAny<NodeId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sim.GetValue("ns=2;s=SimulatedSignal")?.ToString() ?? "");

            var orchestrator = new TestRunOrchestrator(sessionFactoryMock.Object, readWritePortMock.Object);
            var testPlan = new TestPlan
            {
                Id = Guid.NewGuid(),
                TestCases = new List<TestCase>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        RequiredSignals = new List<SignalSnapshot>
                        {
                            new() { SignalName = "ns=2;s=SimulatedSignal" }
                        }
                    }
                }
            };

            var result = await orchestrator.ExecuteTestPlanAsync(testPlan, sim.EndpointUrl, CancellationToken.None);

            Assert.Single(result.Results);
            var caseResult = result.Results[0];
            Assert.True(caseResult.Passed);
            Assert.Equal("99", caseResult.Snapshots[0].Value);
        }

        private class TestOpcUaSession : IOpcUaSession
        {
            public TestOpcUaSession(Opc.Ua.Client.Session session) => InnerSession = session;
            public Opc.Ua.Client.Session InnerSession { get; }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
