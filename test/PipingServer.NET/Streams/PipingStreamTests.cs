using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping.Core.Streams;
using static DebugUtils;

namespace Piping.Streams.Tests
{
    [TestClass]
    public class PipingStreamTests
    {

        [TestMethod, TestCategory("ShortTime")]
        [Ignore]
        public void PipingStreamSyncTest()
        {
            var Encoding = new UTF8Encoding(false);
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}").ToArray();
            using (var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1)))
            using (var Buffers = new DisposableList<CompletableQueueStream>(Enumerable.Range(0, 5).Select(v => new CompletableQueueStream())))
            using (var Piping = new PipingStream(Buffers))
            {
                var Token = TokenSource.Token;
                using (var writer = new StreamWriter(Piping, Encoding, 1024, true))
                {
                    foreach (var Text in Data)
                    {
                        Token.ThrowIfCancellationRequested();
                        writer.WriteLine(Text);
                        Trace.WriteLine($"write: {Text}");
                    }
                }
                foreach (var (os, index) in Buffers.Select((v, i) => (v, i)))
                    using (var reader = new StreamReader(os, Encoding, false, 1024, true))
                        foreach (var ExpectText in Data)
                        {

                            Token.ThrowIfCancellationRequested();
                            var Text = reader.ReadLine();
                            Trace.WriteLine($"cache {index} read: {Text}");
                            Assert.AreEqual(ExpectText, Text);
                        }
            }
        }
        static IEnumerable<object[]> PipingStreamAsyncSyncTestData
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
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(PipingStreamAsyncSyncTestData))]
        public async Task PipingStreamAsyncSyncTest(int receivers)
        {
            var Encoding = new UTF8Encoding(false);
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}\r\n").ToArray();
            using var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1));
            using var Buffers = new DisposableList<CompletableQueueStream>(Enumerable.Range(0, receivers).Select(v => new CompletableQueueStream()));
            using var Piping = new PipingStream(Buffers);
            var Token = TokenSource.Token;
            foreach (var Text in Data)
            {
                Token.ThrowIfCancellationRequested();
                await Piping.WriteAsync(Encoding.GetBytes(Text).AsMemory(), Token);
                Trace.WriteLine($"write: {Text}");
            }
            var ExpectText = Data.Aggregate("", (v1, v2) => v1 + v2);
            var buffer = new byte[Encoding.GetByteCount(ExpectText)].AsMemory();
            foreach (var (os, index) in Buffers.Select((v, i) => (v, i)))
            {
                var _buffer = buffer;
                Token.ThrowIfCancellationRequested();
                while (_buffer.Length > 0)
                {
                    var count = await os.ReadAsync(_buffer, Token);
                    _buffer = _buffer.Slice(count);
                }
                var Text = Encoding.GetString(buffer.Span);
                Trace.WriteLine($"cache {index} read: {Text}");
                Assert.AreEqual(ExpectText, Text);
            }
        }
        static IEnumerable<object[]> PipingStreamAsyncTestData
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
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(PipingStreamAsyncTestData))]
        public async Task PipingStreamAsyncTest(int receivers)
        {
            var Encoding = new UTF8Encoding(false);
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}\r\n").ToArray();
            var Time = TimeSpan.FromMinutes(1);
            var Delay = Time / (5 * 2);
            using var TokenSource = CreateTokenSource(Time);
            using var Buffers = new DisposableList<CompletableQueueStream>(Enumerable.Range(0, receivers).Select(v => new CompletableQueueStream()));
            using var Piping = new PipingStream(Buffers);
            var ExpectText = Data.Aggregate(string.Empty, (v1, v2) => v1 + v2);
            var bufferCount = Encoding.GetByteCount(ExpectText);
            var Token = TokenSource.Token;
            var writerTask = Task.Run(async () =>
            {
                foreach (var Text in Data)
                {
                    Token.ThrowIfCancellationRequested();
                    await Piping.WriteAsync(Encoding.GetBytes(Text).AsMemory(), Token);
                    Trace.WriteLine($"write: '{Text.TrimEnd('\r', '\n')}'");
                    await Task.Delay(Delay, Token);
                }
            });
            var cacheReaderTasks =
                Buffers.Select((os, index) =>
                    Task.Run(async () =>
                    {
                        var buffer = new byte[bufferCount].AsMemory();
                        Token.ThrowIfCancellationRequested();
                        var _buffer = buffer;
                        while (_buffer.Length > 0)
                        {
                            var count = await os.ReadAsync(_buffer, Token);
                            Trace.WriteLine($"{index}: {string.Join(' ', _buffer.Slice(0, count).ToArray().Select(v => $"{v:X2}"))}");
                            _buffer = _buffer.Slice(count);
                        }
                        var Text = Encoding.GetString(buffer.Span);
                        Trace.WriteLine($"cache {index} read: {Text}");
                        Assert.AreEqual(ExpectText, Text);
                    }));
            await Task.WhenAll(new[] { writerTask }.Concat(cacheReaderTasks));
        }
    }
}
