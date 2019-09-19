using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping.Server.Core.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Piping.Server.Core.Pipes.Tests
{
    [TestClass()]
    public class PipingStoreTests
    {
        public ServiceProvider Create(Action<PipingOptions>? Action = null)
        {
            var Factory = new LoggerFactory();
            var Option = new PipingOptions
            {
                BufferSize = 1024 * 4,
                Encoding = Encoding.UTF8,
                WatingTimeout = null,
            };
            var service = new ServiceCollection();
            service.AddLogging();
            if (Action is Action<PipingOptions> _Action)
                service.Configure<PipingOptions>(_Action);
            else
                service.Configure<PipingOptions>(config => {
                    config.BufferSize = 1024 * 4;
                    config.Encoding = Encoding.UTF8;
                    config.WatingTimeout = null;
                });
            service.AddSingleton<PipingStore>();
            return service.BuildServiceProvider();
        }
        [TestMethod()]
        public void PipingStoreTest()
        {
            using var provider = Create();
            var Store = provider.GetRequiredService<PipingStore>();
            Assert.AreEqual(0, Store.Count());
        }

        [TestMethod()]
        public void GetSenderAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetReceiveAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetEnumeratorTest()
        {
            Assert.Fail();
        }
    }
}
