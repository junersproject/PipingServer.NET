using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public async Task SetSenderAsync(RequestKey Key, Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();

            var Waiter = await Store.GetSenderAsync(Key, Token);
            using var finallyremove = Disposable.Create(() => Store.TryRemoveAsync(Waiter));
            using var l = Logger?.LogDebugScope(nameof(SetSenderAsync));
            SetSenderCompletableStream(Waiter, CompletableStream);
            _ = Waiter.SetHeadersAsync(DataTask, Token);
            await SendMessageAsync(CompletableStream.Stream, $"Waiting for {Waiter.RequestedReceiversCount} receiver(s)...", Token);
            await SendMessageAsync(CompletableStream.Stream, $"{Waiter.ReceiversCount} receiver(s) has/have been connected.", Token);
            await Waiter.ReadyAsync(Token);
            _ = _SetSenderAsync(DataTask, CompletableStream, Waiter, Token);
        }
        private async Task _SetSenderAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, ISenderPipe Waiter, CancellationToken Token)
        {
            using var l = Logger?.LogDebugScope(nameof(_SetSenderAsync));
            try
            {
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
        }

        private void SetSenderCompletableStream(IPipe Waiter, ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Sender;
            if(CompletableStream.Stream == CompletableQueueStream.Empty)
                CompletableStream.Stream = new CompletableQueueStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers["Content-Type"] = $"text/plain;charset={Options.Encoding.WebName}";
            CompletableStream.OnFinally += (o, arg) => Store.TryRemoveAsync(Waiter);
        }
        private async Task PipingAsync(Stream RequestStream, CompletableQueueStream InfomationStream, IEnumerable<CompletableQueueStream> Buffers, int BufferSize, CancellationToken Token = default)
        {
            using var l = Logger.LogDebugScope(nameof(PipingAsync));
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
        public async Task SetReceiverAsync(RequestKey Key, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            Logger.LogDebug(nameof(PipingAsync) + " START");
            using var l = Disposable.Create(() => Logger.LogDebug(nameof(PipingAsync) + " STOP"));
            var Waiter = await Store.GetReceiveAsync(Key, Token);
            using var finallyremove = Disposable.Create(() => Store.TryRemoveAsync(Waiter));
            SetReceiverCompletableStream(Waiter, CompletableStream);
            Waiter.AddReceiver(CompletableStream);
            await Task.WhenAny(Waiter.ReadyAsync().AsTask(), Token.AsTask());
        }
        private void SetReceiverCompletableStream(IRecivePipe Waiter, ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Receiver;
            if (CompletableStream.Stream == CompletableQueueStream.Empty)
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
    }
}
