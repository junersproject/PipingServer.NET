using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Piping.Tests
{
    [TestClass]
    public class AsyncMutiPartFormDataEnumerableTests
    {
        [TestMethod]
        public async Task AsyncMutiPartFormDataEnumerable()
        {
            var instance = new AsyncMutiPartFormDataEnumerable();
            await foreach (var (header, stream) in instance)
            {
                Assert.Fail("何故か取得できてしまう");
            }
        }
        static IEnumerable<object[]> GetAsyncEnumerableTestData
        {
            get
            {
                yield return new object[]
                {
                    "test",
                    string.Empty,
                    new string[0],
                };
                yield return new object[]
                {
                    "--------------------------do348x35ddd9489e3\r\n"
                    + "Content-Disposition: form-data; name = \"text1\"\r\n"
                    + "\r\n"
                    + "a & b\r\n"
                    + "--------------------------do348x35ddd9489e3--\r\n",
                    "------------------------do348x35ddd9489e3",
                    new []
                    {
                        "a & b",
                    },
                };
                yield return new object[]
                {
                    "--------------------------do348x35ddd9489e3\r\n"
                    + "Content-Disposition: form-data; name = \"text1\"\r\n"
                    + "\r\n"
                    + "a & b\r\n"
                    + "--------------------------do348x35ddd9489e3\r\n"
                    + "Content - Disposition: form - data; name = \"file1\"; filename = \"hello.txt\"\r\n"
                    + "Content - Type: text / plain\r\n"
                    + "\r\n"
                    + "HELLO\r\n"
                    + "--------------------------do348x35ddd9489e3--\r\n",
                    "------------------------do348x35ddd9489e3",
                    new []
                    {
                        "a & b\r\n",
                        "HELLO\r\n",
                    },
                };
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="FullMesssage">メッセージ全文</param>
        /// <param name="Boundary"></param>
        /// <param name="ExpectMessage">抽出されたメッセージ</param>
        /// <returns></returns>
        [TestMethod,DynamicData(nameof(GetAsyncEnumerableTestData))]
        public async Task GetAsyncEnumerableTest(string FullMesssage, string Boundary, string[] ExpectMessage)
        {
            var Encoding = new UTF8Encoding(false);
            using var Stream = new MemoryStream(Encoding.GetBytes(FullMesssage));
            var enumerable = new AsyncMutiPartFormDataEnumerable(Stream, Encoding, Boundary: Boundary);
            var Count = 0;
            await foreach(var (headers, stream) in enumerable) {
                Count++;
            }
            Assert.AreEqual(ExpectMessage.Length, Count, "取得数が一致しない");
        }
    }
}
