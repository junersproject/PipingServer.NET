using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Piping.Server.App.Tests.Controllers
{
    [TestClass]
    public class PipingControllerTests
    {
        [TestMethod]
        public void SendAndReceiveTest()
        {
            var server = new TestServer(new WebHostBuilder());
            var client = server.CreateClient();
        }
    }
}
