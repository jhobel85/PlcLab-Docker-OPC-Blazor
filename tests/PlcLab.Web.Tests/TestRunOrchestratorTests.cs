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
    public class TestRunOrchestratorTests
    {
        [Fact]
        public async Task ExecuteTestPlanAsync_ReturnsTestRunWithResults()
        {
            // Arrange
            var sessionMock = new Mock<Session>();
            var sessionPortMock = new Mock<IOpcSessionPort>();
            var readWritePortMock = new Mock<IReadWritePort>();
            sessionPortMock.Setup(p => p.CreateSessionAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionMock.Object);
            readWritePortMock.Setup(p => p.ReadValueAsync(sessionMock.Object, It.IsAny<Opc.Ua.NodeId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("42");

            var orchestrator = new TestRunOrchestrator(sessionPortMock.Object, readWritePortMock.Object);
            var testPlan = new TestPlan
            {
                Id = Guid.NewGuid(),
                TestCases = new List<TestCase>
                {
                    new TestCase
                    {
                        Id = Guid.NewGuid(),
                        RequiredSignals = new List<SignalSnapshot>
                        {
                            new SignalSnapshot { SignalName = "Signal1" }
                        }
                    }
                }
            };

            // Act
            var result = await orchestrator.ExecuteTestPlanAsync(testPlan, "opc.tcp://test", CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testPlan.Id, result.TestPlanId);
            Assert.Single(result.Results);
            Assert.True(result.Results[0].Passed);
            Assert.Equal("Signal1", result.Results[0].Snapshots[0].SignalName);
            Assert.Equal("42", result.Results[0].Snapshots[0].Value);
        }
    }
}
