using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PipingServer.Core.Tests
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
                    (PathString)"/PutAndOneGetTest",
                    new QueryCollection(),
                    (PathString)"/putandonegettest",
                    1,
                    "/putandonegettest",
                };
                yield return new object[]
                {
                    (PathString)"/test/test",
                    new QueryCollection(new Dictionary<string, StringValues>{
                        { "n", "2" },
                    }),
                    (PathString)"/test/test",
                    2,
                    "/test/test?n=2"
                };
                yield return new object[]
                {
                    (PathString)"/hoge/fuga",
                    new QueryCollection(new Dictionary<string, StringValues>{
                        { "n", "1" },
                    }),
                    (PathString)"/hoge/fuga",
                    1,
                    "/hoge/fuga"
                };
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(RequestKeyTest1Data))]
        public void RequestKeyTest1(PathString path, IQueryCollection query, PathString ExpectedLocalPath, int ExpectedReceivers, string ExpectToString)
        {
            var Key = new RequestKey(path, query);
            Assert.AreEqual((string)ExpectedLocalPath, Key.Path);
            Assert.AreEqual(ExpectedReceivers, Key.Receivers);
            Assert.AreEqual(ExpectToString, Key.ToString());
        }
    }
}
