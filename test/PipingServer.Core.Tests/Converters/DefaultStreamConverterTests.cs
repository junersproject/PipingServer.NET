using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipingServer.Core.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PipingServer.Core.Converters.Tests
{
    [TestClass()]
    public class DefaultStreamConverterTests
    {
        [TestMethod()]
        public async Task GetStreamTestAsync()
        {
            var Headers = new HeaderDictionary();
            using var Stream = new MemoryStream();
            var (_Headers, _Stream) = await DefaultStreamConverter.GetStreamAsync(Headers, Stream);
            Assert.AreEqual(Headers, _Headers);
            Assert.AreEqual(Stream, _Stream);
        }
        public async Task GetStreamTestErrorAsync()
        {
            var Headers = new HeaderDictionary();
            await Assert.ThrowsExceptionAsync<InvalidEnumArgumentException>(()
               => DefaultStreamConverter.GetStreamAsync(Headers, Stream.Null));
        }

        [TestMethod()]
        public void IsUseTest()
        {
            var converter = new DefaultStreamConverter();
            var Headers = new HeaderDictionary();
            Assert.AreEqual(true, converter.IsUse(Headers));
        }
    }
}
