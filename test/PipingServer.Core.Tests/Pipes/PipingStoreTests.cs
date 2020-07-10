﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipingServer.Core.Options;
using PipingServer.Core.Tests;
using static DebugUtils;

namespace PipingServer.Core.Pipes.Tests
{
    [TestClass()]
    public class PipingStoreTests
    {
        readonly Encoding Encoding = Encoding.UTF8;
        private ServiceProvider CreateProvider(Action<PipingOptions>? OptionEdit = null)
        {
            var services = new ServiceCollection();
            services
                .AddLogging()
                .Configure<PipingOptions>(config =>
                {
                    config.BufferSize = 1024 * 4;
                    config.Encoding = Encoding.UTF8;
                    config.WaitingTimeout = null;
                    OptionEdit?.Invoke(config);
                })
                .AddSingleton<PipingStore>();
            return services.BuildServiceProvider();

        }
        [TestMethod()]
        public void PipingStoreTest()
        {
            using var provider = CreateProvider();
            var Store = provider.GetRequiredService<PipingStore>();
            Assert.AreEqual(0, Store.Count());
        }
        [TestMethod]
        public async Task ConnectionAsyncTestOneToOneFirstSender()
        {
            using var provider = CreateProvider();
            using var TokenSource = CreateTokenSource(TimeSpan.FromSeconds(5));
            var Token = TokenSource.Token;
            var Store = provider.GetRequiredService<PipingStore>();
            var StatusList = new List<MockReadOnlyPipe>();
            Store.OnStatusChanged += (s, arg) =>
            {
                if (!(s is IReadOnlyPipe rop))
                    return;
                Debug.WriteLine($"{s}:{arg.Status}");
                StatusList.Add(new MockReadOnlyPipe(arg, rop) { Status = arg.Status });
            };
            var RequestKey = new RequestKey("/test", new QueryCollection(new Dictionary<string, StringValues>
            {
                { "n", "1"}
            }));
            var SendMessage = "Hello World!";
            var SendContentType = "text/plain; charset=utf-8";
            var SendContentLength = (long)Encoding.GetByteCount(SendMessage);
            var SendStream = (Stream)new MemoryStream(Encoding.GetBytes(SendMessage));
            var SendData = (Headers: (IHeaderDictionary)new HeaderDictionary {
                { "Content-Type", SendContentType },
                { "Content-Length", $"{SendContentLength}" },
            }, Stream: SendStream);
            using (var SenderResult = new MockPipelineStreamResult())
            using (var ReceiverResult = new MockPipelineStreamResult())
            {
                var DataTask = Task.FromResult(SendData);
                var sender = await Store.GetSenderAsync(RequestKey, Token);
                var senderTask = Task.Run(async () =>
                {
                    await sender.ConnectionAsync(DataTask, SenderResult, Token);
                }, Token);
                await Task.Delay(TimeSpan.FromMilliseconds(10), Token);
                var receiverTask = Task.Run(async () =>
                {
                    var receiver = await Store.GetReceiveAsync(RequestKey, Token);
                    await receiver.ConnectionAsync(ReceiverResult, Token);
                }, Token);
                await Task.WhenAll(senderTask, receiverTask);
                Assert.AreEqual(PipeType.Sender, SenderResult.PipeType, "sender pipe type");
                Assert.AreEqual("text/plain; charset=utf-8"
                    , ((SenderResult.Headers?.TryGetValue("Content-Type", out var sct) ?? false) ? sct : StringValues.Empty).ToString()
                    , "sender content-type");
                using var SenderResultStream = new MemoryStream();
                await SenderResult.Stream.CopyToAsync(SenderResultStream);
                Debug.WriteLine("SENDER RESPONSE MESSAGE:");
                Debug.WriteLine(Encoding.GetString(SenderResultStream.ToArray()));
                Assert.AreEqual(PipeType.Receiver, ReceiverResult.PipeType, "receiver pipe type");
                Assert.AreEqual(SendContentType
                    , ((ReceiverResult.Headers?.TryGetValue("Content-Type", out var rct) ?? false) ? rct : StringValues.Empty).ToString()
                    , "receiver content-type");
                Assert.AreEqual(SendContentLength
                    , ReceiverResult.Headers?.ContentLength
                    , "receiver content-length");
                using var ReceiverResultStream = new MemoryStream();
                await ReceiverResult.Stream.CopyToAsync(ReceiverResultStream);
                var ReceiverMessage = Encoding.GetString(ReceiverResultStream.ToArray());
                Debug.WriteLine("RECEIVER RESPONSE MESSAGE:");
                Debug.WriteLine(ReceiverMessage);
                Assert.AreEqual(SendMessage, ReceiverMessage);
                SenderResult.Dispose();
            }
            Debug.WriteLine(nameof(StatusList));
            foreach (var s in StatusList)
                Debug.WriteLine(s);
            var ExpectStatusList = new[] {
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.Wait, Required = PipeType.Receiver, IsRemovable = false, ReceiversCount = 0 },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.Ready, Required = PipeType.None, IsRemovable = false, ReceiversCount = 1 },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.ResponseStart,Required = PipeType.None, IsRemovable =false, ReceiversCount = 1, Headers = SendData.Headers },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.ResponseEnd,Required = PipeType.None, IsRemovable = false, ReceiversCount = 1, Headers = SendData.Headers },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.Dispose,Required = PipeType.None, IsRemovable = true, ReceiversCount = 0, Headers = SendData.Headers },
            };
            Debug.WriteLine(nameof(ExpectStatusList));
            foreach (var s in ExpectStatusList)
                Debug.WriteLine(s);

            CollectionAssert.AreEqual(ExpectStatusList, StatusList, MockReadOnlyPipe.Comparer);
        }
        [TestMethod]
        public async Task ConnectionAsyncTestOneToOneFirstReceiver()
        {
            using var provider = CreateProvider();
            using var TokenSource = CreateTokenSource(TimeSpan.FromSeconds(5));
            var Token = TokenSource.Token;
            var Store = provider.GetRequiredService<PipingStore>();
            var StatusList = new List<MockReadOnlyPipe>();
            Store.OnStatusChanged += (s, arg) =>
            {
                if (!(s is IReadOnlyPipe rop))
                    return;
                Debug.WriteLine($"{s}:{arg.Status}");
                StatusList.Add(new MockReadOnlyPipe(arg, rop) { Status = arg.Status });
            };
            var RequestKey = new RequestKey("/test", new QueryCollection(new Dictionary<string, StringValues>
            {
                { "n", "1"}
            }));
            var SendMessage = "Hello World!";
            var SendContentType = "text/plain; charset=utf-8";
            var SendContentLength = (long)Encoding.GetByteCount(SendMessage);
            var SendStream = (Stream)new MemoryStream(Encoding.GetBytes(SendMessage));
            var SendData = (Headers: (IHeaderDictionary)new HeaderDictionary {
                { "Content-Type", SendContentType },
                { "Content-Length", $"{SendContentLength}" },
            }, Stream: SendStream);
            using (var SenderResult = new MockPipelineStreamResult())
            using (var ReceiverResult = new MockPipelineStreamResult())
            {
                var DataTask = Task.FromResult(SendData);
                var receiver = await Store.GetReceiveAsync(RequestKey, Token);
                var receiverTask = Task.Run(async () =>
                {
                    await receiver.ConnectionAsync(ReceiverResult, Token);
                }, Token);
                await Task.Delay(TimeSpan.FromMilliseconds(10), Token);
                var senderTask = Task.Run(async () =>
                {
                    var sender = await Store.GetSenderAsync(RequestKey, Token);
                    await sender.ConnectionAsync(DataTask, SenderResult, Token);
                }, Token);
                await Task.WhenAll(senderTask, receiverTask);
                Assert.AreEqual(PipeType.Sender, SenderResult.PipeType, "sender pipe type");
                Assert.AreEqual("text/plain; charset=utf-8"
                    , ((SenderResult.Headers?.TryGetValue("Content-Type", out var sct) ?? false) ? sct : StringValues.Empty).ToString()
                    , "sender content-type");
                using var SenderResultStream = new MemoryStream();
                await SenderResult.Stream.CopyToAsync(SenderResultStream);
                Debug.WriteLine("SENDER RESPONSE MESSAGE:");
                Debug.WriteLine(Encoding.GetString(SenderResultStream.ToArray()));
                Assert.AreEqual(PipeType.Receiver, ReceiverResult.PipeType, "receiver pipe type");
                Assert.AreEqual(SendContentType
                    , ((ReceiverResult.Headers?.TryGetValue("Content-Type", out var rct) ?? false) ? rct : StringValues.Empty).ToString()
                    , "receiver content-type");
                Assert.AreEqual(SendContentLength
                    , ReceiverResult.Headers?.ContentLength
                    , "receiver content-length");
                using var ReceiverResultStream = new MemoryStream();
                await ReceiverResult.Stream.CopyToAsync(ReceiverResultStream);
                var ReceiverMessage = Encoding.GetString(ReceiverResultStream.ToArray());
                Debug.WriteLine("RECEIVER RESPONSE MESSAGE:");
                Debug.WriteLine(ReceiverMessage);
                Assert.AreEqual(SendMessage, ReceiverMessage);
                SenderResult.Dispose();
            }
            Debug.WriteLine(nameof(StatusList));
            foreach (var s in StatusList)
                Debug.WriteLine(s);
            var ExpectStatusList = new[] {
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.Wait, Required = PipeType.Sender, IsRemovable = false, ReceiversCount = 1 },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.Ready, Required = PipeType.None, IsRemovable = false, ReceiversCount = 1 },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.ResponseStart, Required = PipeType.None, IsRemovable =false, ReceiversCount = 1, Headers = SendData.Headers },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.ResponseEnd, Required = PipeType.None, IsRemovable = false, ReceiversCount = 1, Headers = SendData.Headers },
                new MockReadOnlyPipe{ Key = RequestKey, Status = PipeStatus.Dispose, Required = PipeType.None, IsRemovable = true, ReceiversCount = 0, Headers = SendData.Headers },
            };
            Debug.WriteLine(nameof(ExpectStatusList));
            foreach (var s in ExpectStatusList)
                Debug.WriteLine(s);

            CollectionAssert.AreEqual(ExpectStatusList, StatusList, MockReadOnlyPipe.Comparer);
        }
    }
}