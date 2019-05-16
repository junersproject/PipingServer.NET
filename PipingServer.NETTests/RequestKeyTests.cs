using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Piping.Tests
{
    [TestClass]
    public class RequestKeyTests
    {
        static IEnumerable<object[]> RequestKeyTest1Data
        {
            get
            {
                yield return new object[]
                {
                    "/PutAndOneGetTest",
                    "",
                    1,
                };
            }
        }
        [TestMethod, DynamicData(nameof(RequestKeyTest1Data))]
        public void RequestKeyTest1(string relativeUri, string ExpectedLocalPath, int ExpectedReceivers)
        {
            var Key = new RequestKey(relativeUri);
            Assert.AreEqual(ExpectedLocalPath, Key.LocalPath);
            Assert.AreEqual(ExpectedReceivers, Key.Receivers);
        }
    }
}
