using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Piping.Tests
{
    [TestClass]
    public class CacheStreamTests
    {

        [TestMethod, TestCategory("ShortTime")]
        public void CacheStreamSyncTest()
        {
            var Encoding = new UTF8Encoding(false);
            using (var TokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
            using (var Base = new MemoryStream())
            using (var Cache = new CacheStream(Base))
            {
                var Token = TokenSource.Token;
                using (var writer = new StreamWriter(Base, Encoding, 1024, true))
                    foreach (var number in Enumerable.Range(0, 5))
                    {
                        Token.ThrowIfCancellationRequested();
                        var Text = $"number:{number}";
                        writer.WriteLine(Text);
                        Trace.WriteLine($"write: {Text}");
                    }
                using (var reader = new StreamReader(Cache, Encoding, false, 1024, true))
                {
                    Token.ThrowIfCancellationRequested();
                    var Text = reader.ReadLine();
                    Trace.WriteLine($"cache read: {Text}");
                }
                foreach (var (os, index) in Cache.OutputStreams.Select((v, i) => (v, i)))
                    using (var reader = new StreamReader(os, Encoding, false, 1024, true))
                    {
                        Token.ThrowIfCancellationRequested();
                        var Text = reader.ReadToEnd();
                        Trace.WriteLine($"cache {index} read: {Text}");
                    }
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task CacheStreamAsyncSyncTest()
        {
            var Encoding = new UTF8Encoding(false);
            using (var TokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
            using (var Base = new MemoryStream())
            using (var Cache = new CacheStream(Base))
            {
                var Token = TokenSource.Token;
                using (var writer = new StreamWriter(Base, Encoding, 1024, true))
                    foreach (var number in Enumerable.Range(0, 5))
                    {
                        Token.ThrowIfCancellationRequested();
                        var Text = $"number:{number}";
                        await writer.WriteLineAsync(Text);
                        Trace.WriteLine($"write: {Text}");
                    }
                using (var reader = new StreamReader(Cache, Encoding, false, 1024, true))
                {
                    Token.ThrowIfCancellationRequested();
                    var Text = await reader.ReadLineAsync();
                    Trace.WriteLine($"cache read: {Text}");
                }
                foreach (var (os, index) in Cache.OutputStreams.Select((v, i) => (v, i)))
                    using (var reader = new StreamReader(os, Encoding, false, 1024, true))
                    {
                        Token.ThrowIfCancellationRequested();
                        var Text = await reader.ReadToEndAsync();
                        Trace.WriteLine($"cache {index} read: {Text}");
                    }
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task CacheStreamAsyncTest()
        {
            var Encoding = new UTF8Encoding(false);
            using (var TokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(1)))
            using (var Base = new MemoryStream())
            using (var Cache = new CacheStream(Base))
            {
                var Token = TokenSource.Token;
                var writerTask = Task.Run(async () =>
                {
                    using (var writer = new StreamWriter(Base, Encoding, 1024, true))
                        foreach (var number in Enumerable.Range(0, 5))
                        {
                            Token.ThrowIfCancellationRequested();
                            var Text = $"number:{number}";
                            await writer.WriteLineAsync(Text);
                            Trace.WriteLine($"write: {Text}");
                        }
                });
                var readerTask = Task.Run(async () =>
                {
                    using (var reader = new StreamReader(Cache, Encoding, false, 1024, true))
                    {
                        Token.ThrowIfCancellationRequested();
                        var Text = await reader.ReadLineAsync();
                        Trace.WriteLine($"cache read: {Text}");
                    }
                });
                var cacheReaderTask = Task.Run(async () =>
                {
                    foreach (var (os, index) in Cache.OutputStreams.Select((v, i) => (v, i)))
                        using (var reader = new StreamReader(os, Encoding, false, 1024, true))
                        {
                            Token.ThrowIfCancellationRequested();
                            var Text = await reader.ReadToEndAsync();
                            Trace.WriteLine($"cache {index} read: {Text}");
                        }
                });
                await Task.WhenAll(writerTask, readerTask, cacheReaderTask);
            }
        }
    }
}
