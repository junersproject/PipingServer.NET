using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DebugUtils;

namespace Piping.Tests
{
    [TestClass]
    public class CacheStreamTests
    {

        [TestMethod, TestCategory("ShortTime")]
        public void CacheStreamSyncTest()
        {
            var Encoding = new UTF8Encoding(false);
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}").ToArray();
            using (var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1)))
            using (var Base = new MemoryStream())
            using (var Cache = new CacheStream(Base))
            {
                var Token = TokenSource.Token;
                using (var writer = new StreamWriter(Base, Encoding, 1024, true))
                {
                    foreach (var Text in Data)
                    {
                        Token.ThrowIfCancellationRequested();
                        writer.WriteLine(Text);
                        Trace.WriteLine($"write: {Text}");
                    }
                }
                Base.Position = 0;
                using (var reader = new StreamReader(Cache, Encoding, false, 1024, false))
                    foreach (var ExpectText in Data)

                    {
                        Token.ThrowIfCancellationRequested();
                        var Text = reader.ReadLine();
                        Trace.WriteLine($"cache read: {Text}");
                        Assert.AreEqual(ExpectText, Text);
                    }
                foreach (var (os, index) in Cache.OutputStreams.Select((v, i) => (v, i)))
                    using (var reader = new StreamReader(os, Encoding, false, 1024, true))
                        foreach(var ExpectText in Data)
                        {

                            Token.ThrowIfCancellationRequested();
                            var Text = reader.ReadLine();
                            Trace.WriteLine($"cache {index} read: {Text}");
                            Assert.AreEqual(ExpectText, Text);
                        }
            }
        }
        static IEnumerable<object[]> CacheStreamAsyncSyncTestData
        {
            get
            {
                yield return new object[]
                {
                    1
                };
                yield return new object[]
                {
                    10
                };
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(CacheStreamAsyncSyncTestData))]
        public async Task CacheStreamAsyncSyncTest(int receivers)
        {
            var Encoding = new UTF8Encoding(false);
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}").ToArray();
            using (var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1)))
            using (var Base = new MemoryStream())
            using (var Cache = new CacheStream(Base, receivers))
            {
                var Token = TokenSource.Token;
                using (var writer = new StreamWriter(Base, Encoding, 1024, true))
                    foreach (var Text in Data)
                    {
                        Token.ThrowIfCancellationRequested();
                        await writer.WriteLineAsync(Text);
                        Trace.WriteLine($"write: {Text}");
                    }
                Base.Position = 0;
                using (var reader = new StreamReader(Cache, Encoding, false, 1024, false))
                    foreach (var ExpectText in Data)
                    {
                        Token.ThrowIfCancellationRequested();
                        var Text = await reader.ReadLineAsync();
                        Trace.WriteLine($"cache read: {Text}");
                        Assert.AreEqual(ExpectText, Text);
                    }
                foreach (var (os, index) in Cache.OutputStreams.Select((v, i) => (v, i)))
                    using (var reader = new StreamReader(os, Encoding, false, 1024, true))
                        foreach (var ExpectText in Data)
                        {
                            Token.ThrowIfCancellationRequested();
                            var Text = await reader.ReadLineAsync();
                            Trace.WriteLine($"cache {index} read: {Text}");
                            Assert.AreEqual(ExpectText, Text);
                        }
            }
        }
        static IEnumerable<object[]> CacheStreamAsyncTestData
        {
            get
            {
                yield return new object[]
                {
                    1
                };
                yield return new object[]
                {
                    10
                };
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(CacheStreamAsyncTestData))]
        public async Task CacheStreamAsyncTest(int receivers)
        {
            var Encoding = new UTF8Encoding(false);
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}").ToArray();
            using (var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1)))
            using (var Base = new MemoryStream())
            using (var Cache = new CacheStream(Base, receivers))
            {
                var Token = TokenSource.Token;
                var writerTask = Task.Run(async () =>
                {
                    using (var writer = new StreamWriter(Base, Encoding, 1024, true))
                        foreach (var Text in Data)
                        {
                            Token.ThrowIfCancellationRequested();
                            await writer.WriteLineAsync(Text);
                            Trace.WriteLine($"write: {Text}");
                        }
                    Base.Position = 0;
                });
                await writerTask;
                var readerTask = Task.Run(async () =>
                {
                    using (var reader = new StreamReader(Cache, Encoding, false, 1024, false))
                        foreach (var ExpectText in Data)
                        {
                            Token.ThrowIfCancellationRequested();
                            var Text = await reader.ReadLineAsync();
                            Trace.WriteLine($"cache read: {Text}");
                            Assert.AreEqual(ExpectText, Text);
                        }
                });
                var cacheReaderTasks = 
                    Cache.OutputStreams.Select((os, index) =>
                        Task.Run(async () =>
                        {
                            using var reader = new StreamReader(os, Encoding, false, 1024, true);
                            foreach (var ExpectText in Data)
                            {
                                Token.ThrowIfCancellationRequested();
                                var Text = await reader.ReadLineAsync();
                                Trace.WriteLine($"cache {index} read: {Text}");
                                Assert.AreEqual(ExpectText, Text);
                            }
                        }));
                await Task.WhenAll(new[] { readerTask }.Concat(cacheReaderTasks));
            }
        }
    }
}
