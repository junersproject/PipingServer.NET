using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Piping.Streams.Tests
{
    [TestClass]
    public class CloseRegisterStreamTests
    {
        /// <summary>
        /// Disposeの動作確認
        /// </summary>
        [TestMethod, TestCategory("ShortTime")]
        public void DisposeTest()
        {
            var isCall = 0;
            using var stream = new MemoryStream();
            using var cstream = new CloseRegisterStream(stream);
            cstream.Closed += (obj, arg) =>
            {
                isCall++;
            };
            Assert.AreEqual(0, isCall, "Dispose non call default is not zero.");
            cstream.Dispose();
            Assert.AreEqual(1, isCall, "Dispose one call not increment.");
            cstream.Dispose();
            Assert.AreEqual(1, isCall, "Dispose two call not event release.");
            var isCall2 = 0;
            cstream.Closed += (obj, arg) =>
            {
                isCall2++;
            };
            Assert.AreEqual(1, isCall2, "Dispose after set and call ?");
        }
        [TestMethod, TestCategory("ShortTime")]
        public void CloseTest()
        {

            var isCall = 0;
            using var stream = new MemoryStream();
            using var cstream = new CloseRegisterStream(stream);
            cstream.Closed += (obj, arg) =>
            {
                isCall++;
            };
            Assert.AreEqual(0, isCall, "Close non call default is not zero.");
            cstream.Close();
            Assert.AreEqual(1, isCall, "Close one call not increment.");
            cstream.Dispose();
            Assert.AreEqual(1, isCall, "Dispose call not event release.");
        }
    }
}
