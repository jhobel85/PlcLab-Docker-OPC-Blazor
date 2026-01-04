using Xunit;
using Moq;
using PlcLab.Web.Pages;
using PlcLab.OPC;
using PlcLab.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace PlcLab.Web.Tests
{
    public class IndexViewModelTests
    {
        [Fact]
        public void TestButtonClick_IncrementsClickCount()
        {
            var vm = new IndexViewModel();
            Assert.Equal(0, vm.ClickCount);
            vm.TestButtonClick();
            Assert.Equal(1, vm.ClickCount);
        }

        [Fact]
        public async Task TryConnectAsync_UpdatesStatus()
        {
            var mockFactory = new Mock<IOpcUaClientFactory>();
            mockFactory.Setup(f => f.CreateSessionAsync(It.IsAny<string>(), false, It.IsAny<System.Threading.CancellationToken>())).ReturnsAsync((Opc.Ua.Client.Session)null!);
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpcUa:Endpoint"]).Returns("opc.tcp://test:4840");
            var vm = new IndexViewModel
            {
                UaFactory = mockFactory.Object,
                Configuration = mockConfig.Object
            };
            await vm.TryConnectAsync();
            Assert.Contains("Connected", vm.OpcStatus);
        }
    }
}
