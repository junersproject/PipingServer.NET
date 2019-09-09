using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping.Server.Core.Pipes;

namespace Piping.Server.Core.Pipe.Tests
{
    [TestClass]
    public class PipingProviderTests
    {
        [TestMethod]
        public void PipingProvider()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<IPipingProvider, PipingProvider>();
            var provider = services.BuildServiceProvider();
            var piping = provider.GetRequiredService<IPipingProvider>();
        }
    }
}
