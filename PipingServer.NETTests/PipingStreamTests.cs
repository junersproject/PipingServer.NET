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
    public class PipingStreamTests
    {

        [TestMethod, TestCategory("ShortTime")]
        public void PipingStreamSyncTest()
        {
            var Encoding = new UTF8Encoding(false);
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}").ToArray();
            using (var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1)))
            using (var Buffers = new DisposableList<BufferStream>(Enumerable.Range(0, 5).Select(v => new BufferStream())))
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
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}").ToArray();
            using (var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1)))
            using (var Buffers = new DisposableList<BufferStream>(Enumerable.Range(0, 5).Select(v => new BufferStream())))
            using (var Piping = new PipingStream(Buffers))
            {
                var Token = TokenSource.Token;
                using (var writer = new StreamWriter(Piping, Encoding, 1024, true))
                    foreach (var Text in Data)
                    {
                        Token.ThrowIfCancellationRequested();
                        writer.WriteLine(Text);
                        Trace.WriteLine($"write: {Text}");
                    }
                foreach (var (os, index) in Buffers.Select((v, i) => (v, i)))
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
            var Data = Enumerable.Range(0, 5).Select(v => $"number: {v}").ToArray();
            using (var TokenSource = CreateTokenSource(TimeSpan.FromMinutes(1)))
            using (var Buffers = new DisposableList<BufferStream>(Enumerable.Range(0, 5).Select(v => new BufferStream())))
            using (var Piping = new PipingStream(Buffers))
            {
                var Token = TokenSource.Token;
                var writerTask = Task.Run(async () =>
                {
                    using (var writer = new StreamWriter(Piping, Encoding, 1024, true))
                        foreach (var Text in Data)
                        {
                            Token.ThrowIfCancellationRequested();
                            await writer.WriteLineAsync(Text);
                            Trace.WriteLine($"write: {Text}");
                        }
                });
                var cacheReaderTasks =
                    Buffers.Select((os, index) =>
                        Task.Run(async () =>
                        {
                            using (var reader = new StreamReader(os, Encoding, false, 1024, true))
                                foreach (var ExpectText in Data)
                                {
                                    Token.ThrowIfCancellationRequested();
                                    var Text = await reader.ReadLineAsync();
                                    Trace.WriteLine($"cache {index} read: {Text}");
                                    Assert.AreEqual(ExpectText, Text);
                                }
                        }));
                await Task.WhenAll(new[] { writerTask }.Concat(cacheReaderTasks));
            }
        }
    }
}
