using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PipingServer.Mvc.Converters.Tests
{
    [TestClass()]
    public class DefaultStreamConverterTests
    {
        [TestMethod()]
        public async Task GetStreamAsyncTestAsync()
        {
            var Headers = new Dictionary<string, StringValues>();
            var Body = new MemoryStream();
            var (_Headers, _Body) = await DefaultStreamConverter.GetStreamAsync(Headers, Body);
            Assert.AreEqual(Headers, _Headers);
            Assert.AreEqual(Body, _Body);
        }
    }
}
