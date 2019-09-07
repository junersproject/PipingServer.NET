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
using Piping.Core.Converters;
using Piping.Core.Internal;
using Piping.Core.Streams;

namespace Piping.Core.Pipes
{
    public partial class PipingProvider : IPipingProvider
    {
        readonly PipingOptions Options;
        readonly ILogger<PipingProvider> Logger;
        readonly IEnumerable<IStreamConverter> Converters;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Logger"></param>
        public PipingProvider(ILogger<PipingProvider> Logger, IEnumerable<IStreamConverter> Converters, IOptions<PipingOptions> Options)
            => (this.Logger, this.Converters, this.Options) = (Logger, Converters, Options?.Value ?? throw new ArgumentNullException(nameof(Options)));
        public void SetSender(string Path, HttpRequest Request, ICompletableStream CompletableStream, CancellationToken Token = default)
            => SetSender(new RequestKey(Path), Request, CompletableStream, Token);
        public void SetSender(RequestKey Key, HttpRequest Request, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            // seek request body
            if ((Request ?? throw new ArgumentNullException(nameof(Request))).Body.CanSeek)
                Request.Body.Seek(0, SeekOrigin.Begin);

            var Waiter = Get(Key);
            using var finallyremove = Disposable.Create(() => TryRemove(Waiter));
            Waiter.AssertKey(Key);
            Logger.LogDebug(nameof(SetSender) + " START");
            using var l = Disposable.Create(() => Logger.LogDebug(nameof(SetSender) + " STOP"));
            var DataTask = GetDataAsync(Request, Token);
            SetSenderCompletableStream(Waiter, CompletableStream);
            Waiter.SetSenderComplete();
            SendMessage(CompletableStream.Stream, $"Waiting for {Waiter.RequestedReceiversCount} receiver(s)...");
            SendMessage(CompletableStream.Stream, $"{Waiter.ReceiversCount} receiver(s) has/have been connected.");
            _ = Task.Run(async () =>
            {
                Logger.LogDebug("async " + nameof(SetSender) + " START");
                using var l = Disposable.Create(() => Logger.LogDebug("async " + nameof(SetSender) + " STOP"));
                try
                {
                    if (Waiter.IsReady)
                        Waiter.ReadyTaskSource.TrySetResult(true);
                    else
                        using (Token.Register(() => Waiter.ReadyTaskSource.TrySetCanceled(Token)))
                            await Waiter.ReadyTaskSource.Task;
                    var (Headers, Stream) = await DataTask;
                    SetHeaders(Waiter.Receivers, Headers);
                    await SendMessageAsync(CompletableStream.Stream, $"Start sending with {Waiter.ReceiversCount} receiver(s)!");
                    var PipingTask = PipingAsync(Stream, CompletableStream.Stream, Waiter.Receivers.Select(v => v.Stream), 1024, Options.Encoding, Token);
                    Waiter.ResponseTaskSource.TrySetResult(true);
                    await Task.WhenAll(Waiter.ResponseTaskSource.Task, PipingTask);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, nameof(SetSender));
                    Waiter.ResponseTaskSource.TrySetException(e);
                }
            });
        }
        private void SetSenderCompletableStream(Pipe Waiter, ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Sender;
            CompletableStream.Stream = new CompletableQueueStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers["Content-Type"] = $"text/plain;charset={Options.Encoding.WebName}";
            CompletableStream.OnFinally += (o, arg) => TryRemove(Waiter);
        }
        private void SetHeaders(IEnumerable<ICompletableStream> Responses, IHeaderDictionary Headers)
        {
            foreach (var r in Responses)
                if (r.Headers is IHeaderDictionary _Headers)
                    foreach (var kv in Headers)
                        if (!_Headers.TryGetValue(kv.Key, out _ ))
                            _Headers[kv.Key] = kv.Value;
        }
        private Task<(IHeaderDictionary Headers, Stream Stream)> GetDataAsync(HttpRequest Request, CancellationToken Token = default)
        {
            foreach (var c in Converters)
                if (c.IsUse(Request.Headers))
                    return c.GetStreamAsync(Request.Headers, Request.Body, Token);
            return DefaultStreamConverter.GetStreamAsync(Request.Headers, Request.Body, Token);
        }
        private async Task PipingAsync(Stream RequestStream, CompletableQueueStream InfomationStream, IEnumerable<CompletableQueueStream> Buffers, int BufferSize, Encoding Encoding, CancellationToken Token = default)
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
            using var writer = new StreamWriter(InfomationStream, Encoding, BufferSize, true);
        }
        public void SetReceiver(string RelativeUri, ICompletableStream CompletableStream, CancellationToken Token = default)
            => SetReceiver(new RequestKey(RelativeUri), CompletableStream, Token);
        public void SetReceiver(RequestKey Key, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            Logger.LogDebug(nameof(PipingAsync) + " START");
            using var l = Disposable.Create(() => Logger.LogDebug(nameof(PipingAsync) + " STOP"));
            var Waiter = Get(Key);
            using var finallyremove = Disposable.Create(() => TryRemove(Waiter));
            Waiter.AssertKey(Key);
            if (!(Waiter.ReceiversCount < Waiter.RequestedReceiversCount))
                throw new InvalidOperationException($"Connection receivers over.");
            SetReceiverCompletableStream(Waiter, CompletableStream);
            Waiter.AddReceiver(CompletableStream);
            if (Waiter.IsReady)
                Waiter.ReadyTaskSource.TrySetResult(true);
        }
        private void SetReceiverCompletableStream(Pipe Waiter, ICompletableStream CompletableStream)
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
                TryRemove(Waiter);
            };
            CompletableStream.HeaderIsSetCompletedTask = Waiter.HeaderIsSetCompletedTask;
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
        protected Pipe Get(RequestKey Key)
        {
            lock (_waiters)
            {
                if (_waiters.TryGetValue(Key, out var Waiter))
                {
                    Logger.LogDebug("GET " + Waiter);
                }
                else
                {
                    Waiter = new Pipe(Key, Options);
                    Logger.LogDebug("CREATE " + Waiter);
                    _waiters.Add(Key, Waiter);
                    Waiter.OnWaitTimeout += (o, arg) =>
                    {
                        TryRemove(Waiter);
                    };
                }
                return Waiter;
            }
        }
        protected bool TryRemove(Pipe Waiter)
        {
            lock (_waiters)
            {
                bool Result;
                if (Result = Waiter.IsRemovable)
                {
                    Logger.LogDebug("REMOVE " + Waiter);
                    _waiters.Remove(Waiter.Key);
                    Waiter.Dispose();
                }
                else
                {
                    Logger.LogDebug("KEEP " + Waiter);
                }
                return Result;
            }
        }
        protected Dictionary<RequestKey, Pipe> _waiters = new Dictionary<RequestKey, Pipe>();

        public IEnumerator<IPipe> GetEnumerator() => _waiters.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var value in _waiters.Values)
                        value.Dispose();
                }
                disposedValue = true;
            }
        }
        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
