using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Piping.Server.Core.Internal;
using Piping.Server.Core.Streams;

namespace Piping.Server.Core.Pipes
{
    public partial class PipingProvider : IPipingProvider
    {
        readonly PipingOptions Options;
        readonly ILogger<PipingProvider> Logger;
        readonly IPipingStore Store;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Logger"></param>
        public PipingProvider(ILogger<PipingProvider> Logger, IOptions<PipingOptions> Options, IPipingStore Store)
            => (this.Logger, this.Options, this.Store) = (Logger, Options?.Value ?? throw new ArgumentNullException(nameof(Options)), Store);
        public async Task SetSenderAsync(string Path, Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();

            var Waiter = await Store.GetAsync(Path, Token);
            using var finallyremove = Disposable.Create(() => Store.TryRemoveAsync(Waiter));
            Waiter.AssertKey();
            Logger.LogDebug(nameof(SetSenderAsync) + " START");
            using var l = Disposable.Create(() => Logger.LogDebug(nameof(SetSenderAsync) + " STOP"));
            SetSenderCompletableStream(Waiter, CompletableStream);
            Waiter.SetSenderComplete();
            SendMessage(CompletableStream.Stream, $"Waiting for {Waiter.RequestedReceiversCount} receiver(s)...");
            SendMessage(CompletableStream.Stream, $"{Waiter.ReceiversCount} receiver(s) has/have been connected.");
            _ = Task.Run(async () =>
            {
                Logger.LogDebug("async " + nameof(SetSenderAsync) + " START");
                using var l = Disposable.Create(() => Logger.LogDebug("async " + nameof(SetSenderAsync) + " STOP"));
                try
                {
                    await Waiter.ReadyAsync(Token);
                    var (Headers, Stream) = await DataTask;
                    var PipingTask = PipingAsync(Stream, CompletableStream.Stream, Waiter.Receivers.Select(v => v.Stream), Options.BufferSize, Token);
                    var SetHeaderTask = Waiter.SetHeadersAsync(Receivers => {
                        Receivers.SetHeaders(Headers);
                        return Task.CompletedTask;
                    });
                    await SendMessageAsync(CompletableStream.Stream, $"Start sending with {Waiter.ReceiversCount} receiver(s)!");
                    await Task.WhenAll(PipingTask, SetHeaderTask.AsTask());
                }
                catch (Exception e)
                {
                    Logger.LogError(e, nameof(SetSenderAsync));
                }
            });
        }
        private void SetSenderCompletableStream(IPipe Waiter, ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Sender;
            CompletableStream.Stream = new CompletableQueueStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers["Content-Type"] = $"text/plain;charset={Options.Encoding.WebName}";
            CompletableStream.OnFinally += (o, arg) => Store.TryRemoveAsync(Waiter);
        }
        private async Task PipingAsync(Stream RequestStream, CompletableQueueStream InfomationStream, IEnumerable<CompletableQueueStream> Buffers, int BufferSize, CancellationToken Token = default)
        {
            Logger.LogDebug(nameof(PipingAsync) + " START");
            using var l = Disposable.Create(() => Logger.LogDebug(nameof(PipingAsync) + " STOP"));
            var buffer = new byte[BufferSize].AsMemory();
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            var byteCounter = 0L;
            using var finallyact = Disposable.Create(() =>
            {
                foreach (var b in Buffers)
                    b.CompleteAdding();
                InfomationStream.CompleteAdding();
            });
            while ((bytesRead = await RequestStream.ReadAsync(buffer, Token).ConfigureAwait(false)) != 0)
            {
                await Stream.WriteAsync(buffer.Slice(0, bytesRead), Token).ConfigureAwait(false);
                byteCounter += bytesRead;
            }
            await SendMessageAsync(InfomationStream, $"Sending successful! {byteCounter} bytes.");
        }
        public async Task SetReceiverAsync(string Path, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            Logger.LogDebug(nameof(PipingAsync) + " START");
            using var l = Disposable.Create(() => Logger.LogDebug(nameof(PipingAsync) + " STOP"));
            var Waiter = await Store.GetAsync(Path, Token);
            using var finallyremove = Disposable.Create(() => Store.TryRemoveAsync(Waiter));
            Waiter.AssertKey();
            if (!(Waiter.ReceiversCount < Waiter.RequestedReceiversCount))
                throw new InvalidOperationException($"Connection receivers over.");
            SetReceiverCompletableStream(Waiter, CompletableStream);
            Waiter.AddReceiver(CompletableStream);
            await Task.WhenAny(Waiter.ReadyAsync().AsTask(), Token.AsTask());
        }
        private void SetReceiverCompletableStream(IPipe Waiter, ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Receiver;
            CompletableStream.Stream = new CompletableQueueStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers["Access-Control-Allow-Origin"] = " * ";
            CompletableStream.Headers["Access-Control-Expose-Headers"] = "Content-Length, Content-Type";
            CompletableStream.OnFinally += (o, arg) =>
            {
                var Removed = Waiter.RemoveReceiver(CompletableStream);
                Logger.LogDebug("STREAM REMOVE " + (Removed ? "SUCCESS" : "FAILED"));
                Store.TryRemoveAsync(Waiter);
            };
        }
        private async Task SendMessageAsync(Stream Stream, string Message, CancellationToken Token = default)
        {
            Logger.LogDebug(Message);
            await Stream.WriteAsync(Options.Encoding.GetBytes("[INFO] " + Message + Environment.NewLine).AsMemory(), Token);
        }
        private void SendMessage(Stream Stream, string Message)
        {
            Logger.LogDebug(Message);
            Stream.Write(Options.Encoding.GetBytes("[INFO]" + Message + Environment.NewLine).AsSpan());
        }

        public IEnumerator<IPipe> GetEnumerator() => Store.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
