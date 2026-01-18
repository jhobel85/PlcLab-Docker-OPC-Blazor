using Allure.Xunit.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Opc.Ua.Client;
using PlcLab.Application;
using PlcLab.Application.Ports;
using PlcLab.Domain;
using Xunit;

namespace PlcLab.Web.Tests
{
    [AllureSuite("TestRunOrchestrator")]
    public class TestRunOrchestratorTests        
    {
        [Fact]
            [AllureFeature("Test Plan Execution")]
        public async Task ExecuteTestPlanAsync_ReturnsTestRunWithResults()
        {
            // Arrange
#pragma warning disable SYSLIB0050 // Type or member is obsolete
            var realSession = (Opc.Ua.Client.Session)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Opc.Ua.Client.Session));
#pragma warning restore SYSLIB0050 // Type or member is obsolete
            var opcUaSessionStub = new TestOpcUaSession(realSession);

            var sessionFactoryMock = new Mock<IOpcUaSessionFactory>();
            sessionFactoryMock.Setup(f => f.CreateSessionAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(opcUaSessionStub);

            var readWritePortMock = new Mock<IReadWritePort>();
            readWritePortMock.Setup(p => p.ReadValueAsync(realSession, It.IsAny<Opc.Ua.NodeId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("42");

            var orchestrator = new TestRunOrchestrator(sessionFactoryMock.Object, readWritePortMock.Object);
            var testPlan = new TestPlan
            {
                Id = Guid.NewGuid(),
                TestCases = new List<TestCase>
                {
                    new() {
                        Id = Guid.NewGuid(),
                        RequiredSignals = new List<SignalSnapshot>
                        {
                            // Use a valid OPC UA NodeId string
                            new SignalSnapshot { SignalName = "ns=2;s=Signal1" }
                        }
                    }
                }
            };

            // Act
            var result = await orchestrator.ExecuteTestPlanAsync(testPlan, "opc.tcp://test", CancellationToken.None);

            // Debug output

            if (result.Results.Count > 0)
            {
                var res = result.Results[0];
                if (!res.Passed)
                {
                    var msg = $"TestCaseId: {res.TestCaseId}, Passed: {res.Passed}, Message: {res.Message}, Snapshots: {res.Snapshots.Count}";
                    if (res.Snapshots.Count > 0)
                        msg += $", Snapshot Value: {res.Snapshots[0].Value}";
                    throw new Exception($"Test failed debug info: {msg}");
                }
            }

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testPlan.Id, result.TestPlanId);
            Assert.Single(result.Results);
            Assert.True(result.Results[0].Passed);
            Assert.Equal("ns=2;s=Signal1", result.Results[0].Snapshots[0].SignalName);
            Assert.Equal("42", result.Results[0].Snapshots[0].Value);
        }

        private class TestOpcUaSession : IOpcUaSession
        {
            public TestOpcUaSession(Opc.Ua.Client.Session session) => InnerSession = session;
            public Opc.Ua.Client.Session InnerSession { get; }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
