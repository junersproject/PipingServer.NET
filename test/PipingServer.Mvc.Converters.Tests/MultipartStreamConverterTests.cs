using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static DebugUtils;
using IHeaderDictionary = System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>;
using IEnumerableHeader = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>;
using HeaderDictionary = System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>;
using System.Linq;

namespace PipingServer.Mvc.Converters.Tests
{
    [TestClass()]
    public class MultipartStreamConverterTests
    {
        [TestMethod]
        public void MultipartStreamConverterTest()
        {
            var Option = OptionsCreate(new MultipartStreamConverterOption
            {

            });
            var Converter = new MultipartStreamConverter(Option);
        }
        static IEnumerable<object[]> IsUseTestData
        {
            get
            {
                var Compare = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                yield return IsUseTest(new MultipartStreamConverter(OptionsCreate(new MultipartStreamConverterOption
                {
                })), new HeaderDictionary(Compare)
                {
                }, false);
                yield return IsUseTest(new MultipartStreamConverter(OptionsCreate(new MultipartStreamConverterOption
                {
                })), new HeaderDictionary(Compare)
                {
                    { "Content-Type", "multipart/form-data" }
                }, true);
                static object[] IsUseTest(MultipartStreamConverter Converter, IHeaderDictionary Headers, bool ExpectedResult)
                    => new object[] { Converter, Headers, ExpectedResult };
            }
        }

        [TestMethod, DynamicData(nameof(IsUseTestData))]
        public void IsUseTest(MultipartStreamConverter Converter, IHeaderDictionary Headers, bool ExpectedResult)
            => Assert.AreEqual(ExpectedResult, Converter.IsUse<IHeaderDictionary>(Headers));
        static IEnumerable<object[]> GetStreamAsyncTestData
        {
            get
            {
                var Encoding = System.Text.Encoding.UTF8;
                var Compare = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
                yield return GetStreamAsyncTest(
                    new MultipartStreamConverter(OptionsCreate(new MultipartStreamConverterOption
                    {
                    })), new HeaderDictionary(Compare)
                {
                    { "Content-Type", "multipart/form-data; boundary=---------------------------41952539122868" },
                }, StringToStream(TrimAllLines(
                    @"-----------------------------41952539122868
                    Content-Disposition: form-data; name=""multilined""
                    line 1
                    line 2
                    line 3
                    -----------------------------41952539122868--"))
                , new HeaderDictionary(Compare)
                {
                    {"Content-Disposition", "form-data; name=\"multilined\"" },
                }, Encoding.GetBytes(TrimAllLines(
                       "line 1\r\nline 2\r\nline 3")));
                static object[] GetStreamAsyncTest(MultipartStreamConverter Converter, IHeaderDictionary Headers, Stream Body, IEnumerableHeader ExpectedHeaders, byte[] ExpectedBody)
                    => new object[] { Converter, Headers, Body, ExpectedHeaders, ExpectedBody };
            }
        }
        [TestMethod, DynamicData(nameof(GetStreamAsyncTestData)), Ignore]
        public async Task GetStreamAsyncTestAsync(MultipartStreamConverter Converter, IHeaderDictionary Headers, Stream Body, IEnumerableHeader ExpectedHeaders, byte[] ExpectedBody)
        {
            using var TokenSource = CreateTokenSource(TimeSpan.FromSeconds(30));
            using (Body)
            {
                var (_Header, _Body) = await Converter.GetStreamAsync(Headers, Body, TokenSource.Token);
                CollectionAssert.AreEqual(
                    ExpectedHeaders.OrderBy(v => v.Key).ToArray()
                    , _Header.OrderBy(v => v.Key).ToArray());
                using var __Body = new MemoryStream();
                await _Body.CopyToAsync(__Body, TokenSource.Token);
                __Body.Position = 0;
                CollectionAssert.AreEqual(
                    ExpectedBody,
                    __Body.ToArray()
                    );
            }
        }
        static string TrimAllLines(string input)
        {
            return
                string.Concat(
                    input.Split('\n')
                         .Select(x => x.Trim())
                         .Aggregate((first, second) => first + '\n' + second)
                         .Where(x => x != '\r'));
        }
        public static Stream StringToStream(string input)
        {
            return StringToStream(input, Encoding.UTF8);
        }

        public static Stream StringToStream(string input, Encoding encoding)
        {
            var stream = new MemoryStream();
#pragma warning disable IDE0067 // Dispose objects before losing scope
            var writer = new StreamWriter(stream, encoding);
#pragma warning restore IDE0067 // Dispose objects before losing scope
            writer.Write(input);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
