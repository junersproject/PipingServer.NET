using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping.Server.Core.Tests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DebugUtils;

namespace Piping.Server.Core.Pipes.Tests
{
    [TestClass()]
    public class PipingStoreTests
    {
        IServiceProvider? provider = null;
        readonly Encoding Encoding = Encoding.UTF8;
        [TestInitialize]
        public void Initialize()
        {
            var services = new ServiceCollection();
            services
                .AddLogging()
                .Configure<PipingOptions>( config => {
                    config.BufferSize = 1024 * 4;
                    config.Encoding = Encoding.UTF8;
                    config.WaitingTimeout = null;
                })
                .AddSingleton<PipingStore>();
            provider = services.BuildServiceProvider();
        }
        [TestCleanup]
        public void Cleanup()
        {
            if (provider is IDisposable Disposable)
                Disposable.Dispose();
            provider = null;
        }
        [TestMethod()]
        public void PipingStoreTest()
        {
            var Store = provider.GetRequiredService<PipingStore>();
            Assert.AreEqual(0, Store.Count());
        }

        [TestMethod]
        public async Task ConnectionAsyncTestOneToOne()
        {
            var TokenSource = CreateTokenSource(TimeSpan.FromSeconds(5));
            var Store = provider.GetRequiredService<PipingStore>();
            var StatusList = new List<PipeStatus>();
            Store.OnStatusChanged += (s, arg) =>
            {
                Debug.WriteLine($"{s}:{arg.Status}");
                StatusList.Add(arg.Status);
            };
            using (var SenderResult = new MockPipelineStreamResult())
            using (var ReceiverResult = new MockPipelineStreamResult())
            {
                var SendMessage = "Hello World!";
                var SendStream = (Stream)new MemoryStream(Encoding.GetBytes(SendMessage));
                var RequestKey = new RequestKey("/test", new QueryCollection(new Dictionary<string, StringValues>
            {
                { "n", "1"}
            }));
                var DataTask = Task.FromResult((Headers: (IHeaderDictionary)new HeaderDictionary {
                {"Content-Type", "text/plain; charset=utf-8" }
            }, Stream: SendStream));
                var senderTask = Task.Run(async () =>
                {
                    var sender = await Store.GetSenderAsync(RequestKey, TokenSource.Token);
                    await sender.ConnectionAsync(DataTask, SenderResult, TokenSource.Token);
                });
                var receiverTask = Task.Run(async () =>
                {
                    var receiver = await Store.GetReceiveAsync(RequestKey, TokenSource.Token);
                    await receiver.ConnectionAsync(ReceiverResult, TokenSource.Token);
                });
                await Task.WhenAll(senderTask, receiverTask);
                Assert.AreEqual(PipeType.Sender, SenderResult.PipeType, "sender pipe type");
                Assert.AreEqual("text/plain;charset=utf-8"
                    , ((SenderResult.Headers?.TryGetValue("Content-Type", out var sct) ?? false) ? sct : StringValues.Empty).ToString()
                    , "sender content-type");
                using var SenderResultStream = new MemoryStream();
                await SenderResult.Stream.CopyToAsync(SenderResultStream);
                Debug.WriteLine("SENDER RESPONSE MESSAGE:");
                Debug.WriteLine(Encoding.GetString(SenderResultStream.ToArray()));
                Assert.AreEqual(PipeType.Receiver, ReceiverResult.PipeType, "receiver pipe type");
                Assert.AreEqual("text/plain; charset=utf-8"
                    , ((ReceiverResult.Headers?.TryGetValue("Content-Type", out var rct) ?? false) ? rct : StringValues.Empty).ToString()
                    , "receiver content-type");
                using var ReceiverResultStream = new MemoryStream();
                await ReceiverResult.Stream.CopyToAsync(ReceiverResultStream);
                var ReceiverMessage = Encoding.GetString(ReceiverResultStream.ToArray());
                Debug.WriteLine("RECEIVER RESPONSE MESSAGE:");
                Debug.WriteLine(ReceiverMessage);
                Assert.AreEqual(SendMessage, ReceiverMessage);
                SenderResult.Dispose();
            }
            CollectionAssert.AreEqual(new[] {
                PipeStatus.Wait,
                PipeStatus.Ready,
                PipeStatus.ResponseStart,
                PipeStatus.ResponseEnd,
                PipeStatus.Dispose
            }, StatusList);
        }
        [TestMethod()]
        public void GetSenderAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetReceiveAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RemoveAsyncTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetEnumeratorTest()
        {
            Assert.Fail();
        }
    }
}
