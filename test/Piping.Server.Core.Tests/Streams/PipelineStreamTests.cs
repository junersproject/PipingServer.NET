using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping.Server.Core.Streams;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Piping.Server.Core.Streams.Tests
{
    [TestClass()]
    public class PipelineStreamTests
    {
        readonly Encoding Encoding = Encoding.UTF8;
        [TestMethod]
        public void CompleteTest()
        {
            using var Stream = new PipelineStream();
            var WriteMessage1 = "HELLO";
            Stream.Write(Encoding.GetBytes(WriteMessage1));
            Stream.Flush();
            var ReadCount = Encoding.GetByteCount(WriteMessage1);
            var ReadBytes = new byte[ReadCount];
            ReadCount = Stream.Read(ReadBytes);
            var ReadMessage1 = Encoding.GetString(ReadBytes.AsSpan().Slice(0, ReadCount));
            Assert.AreEqual(WriteMessage1, ReadMessage1);

            Stream.Complete();
            var WriteMessage2 = "HI!";
            Assert.ThrowsException<InvalidOperationException>(()=> Stream.Write(Encoding.GetBytes(WriteMessage2)));
        }

        [TestMethod]
        public async Task CompleteAsyncTest()
        {
            using var Stream = new PipelineStream();
            var WriteMessage1 = "HELLO";
            await Stream.WriteAsync(Encoding.GetBytes(WriteMessage1));
            await Stream.FlushAsync();
            var ReadCount = Encoding.GetByteCount(WriteMessage1);
            var ReadBytes = new byte[ReadCount];
            ReadCount = await Stream.ReadAsync(ReadBytes);
            var ReadMessage1 = Encoding.GetString(ReadBytes.AsSpan().Slice(0, ReadCount));
            Assert.AreEqual(WriteMessage1, ReadMessage1);

            Stream.Complete();
            var WriteMessage2 = "HI!";
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async() => await Stream.WriteAsync(Encoding.GetBytes(WriteMessage2)));
        }

        [TestMethod]
        public void PipelineStreamTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void PipelineStreamTest1()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void FlushTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void FlushAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void ReadAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void ReadAsyncTest1()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void ReadTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void ReadTest1()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void SeekTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void SetLengthTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void WriteAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void WriteAsyncTest1()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void WriteTest()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void WriteTest1()
        {
            Assert.Fail();
        }
    }
}
