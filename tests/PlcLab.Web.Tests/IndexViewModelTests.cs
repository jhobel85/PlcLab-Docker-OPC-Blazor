using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using PlcLab.Infrastructure;
using PlcLab.Domain;
using PlcLab.Web.Services;
using PlcLab.Web.ViewModel;
using Xunit;

namespace PlcLab.Web.Tests
{
    public class IndexViewModelTests
    {

        [Fact]
        public void TestButtonClick_IncrementsClickCount()
        {
            var (vm, _, _) = CreateViewModel();
            Assert.Equal(0, vm.ClickCount);
            vm.TestButtonClick();
            Assert.Equal(1, vm.ClickCount);
        }

        [Fact]
        public async Task TryConnectAsync_UpdatesStatus()
        {
            var (vm, connectionMock, seedMock) = CreateViewModel();
            connectionMock.Setup(c => c.TryConnectAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            connectionMock.SetupGet(c => c.Status).Returns("Connected");
            connectionMock.SetupGet(c => c.SessionVersion).Returns(1);
            seedMock.Setup(s => s.LoadSeedInfoAsync(It.IsAny<Opc.Ua.Client.Session?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SeedInfo { SeedEnabled = false, Variables = new List<SeedVariable>() });

            await vm.TryConnectAsync();
            Assert.Contains("Connected", vm.OpcStatus);
        }

        private static (IndexViewModel vm, Mock<IOpcConnectionService> connectionMock, Mock<ISeedDataClient> seedMock) CreateViewModel()
        {
            var connectionMock = new Mock<IOpcConnectionService>();
            connectionMock.SetupGet(c => c.Status).Returns("Not connected");
            connectionMock.SetupGet(c => c.ClientApplicationName).Returns("TestApp");
            connectionMock.SetupGet(c => c.CurrentSession).Returns((Opc.Ua.Client.Session?)null);
            connectionMock.SetupGet(c => c.IsConnecting).Returns(false);
            connectionMock.SetupGet(c => c.SessionVersion).Returns(0);

            var seedMock = new Mock<ISeedDataClient>();
            seedMock.Setup(s => s.LoadSeedInfoAsync(It.IsAny<Opc.Ua.Client.Session?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SeedInfo { SeedEnabled = false, Variables = new List<SeedVariable>() });
            seedMock.Setup(s => s.InvokeAddAsync(It.IsAny<Opc.Ua.Client.Session?>(), It.IsAny<float>(), It.IsAny<uint>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0d);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["OpcUa:Endpoint"] = "opc.tcp://test:4840"
                })
                .Build();

            var vm = new IndexViewModel(connectionMock.Object, seedMock.Object, config);
            return (vm, connectionMock, seedMock);
        }
    }
}
