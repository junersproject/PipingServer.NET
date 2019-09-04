using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Piping.Converters;
using Piping.Streams;

namespace Piping.Models
{
    public partial class PipingProvider : IPipingProvider
    {
        readonly Encoding Encoding;
        readonly IServiceProvider Services;
        readonly PipingOptions Options;
        readonly ILogger<PipingProvider> Logger;
        readonly IEnumerable<IStreamConverter> Converters;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Logger"></param>
        public PipingProvider(IServiceProvider Services, ILogger<PipingProvider> Logger, Encoding Encoding, IEnumerable<IStreamConverter> Converters, IOptions<PipingOptions> Options)
            => (this.Services, this.Logger, this.Encoding, this.Converters, this.Options) = (Services, Logger, Encoding, Converters, Options.Value);
        public IActionResult AddSender(string RelativeUri, HttpRequest Request, CancellationToken Token = default)
            => AddSender(new RequestKey(RelativeUri), Request, Token);
        public IActionResult AddSender(RequestKey Key, HttpRequest Request, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            // seek request body
            if (Request.Body.CanSeek)
                Request.Body.Seek(0, SeekOrigin.Begin);

            var Waiter = Get(Key);
            using var finallyremove = Disposable.Create(() => TryRemove(Waiter));
            Waiter.AssertKey(Key);
            using var l = Logger.BeginLogDebugScope(nameof(AddSender));
            Waiter.SetSenderComplete();
            var Response = GetSenderStreamResult(Waiter);
            var DataTask = GetDataAsync(Request, Token);
            SendMessage(Response.Stream, $"Waiting for {Waiter.RequestedReceiversCount} receiver(s)...");
            SendMessage(Response.Stream, $"{Waiter.ReceiversCount} receiver(s) has/have been connected.");
            _ = Task.Run(async () =>
            {
                using var l = Logger.BeginLogDebugScope("async " + nameof(AddSender) + " Run");
                try
                {
                    if (Waiter.IsReady)
                        Waiter.ReadyTaskSource.TrySetResult(true);
                    else
                        using (Token.Register(() => Waiter.ReadyTaskSource.TrySetCanceled(Token)))
                            await Waiter.ReadyTaskSource.Task;
                    var (Stream, ContentLength, ContentType, ContentDisposition) = await DataTask;
                    SetHeaders(Waiter.Receivers, ContentLength, ContentType, ContentDisposition);
                    await SendMessageAsync(Response.Stream, $"Start sending with {Waiter.ReceiversCount} receiver(s)!");
                    var PipingTask = PipingAsync(Stream, Response.Stream, Waiter.Receivers.Select(v => v.Stream), 1024, Encoding, Token);
                    Waiter.ResponseTaskSource.TrySetResult(true);
                    await Task.WhenAll(Waiter.ResponseTaskSource.Task, PipingTask);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, nameof(AddSender));
                    Waiter.ResponseTaskSource.TrySetException(e);
                }
            });
            return Response;
        }
        private CompletableStreamResult GetSenderStreamResult(Pipe Waiter)
        {
            var Result = Services.GetRequiredService<CompletableStreamResult>();
            Result.Identity = "Sender";
            Result.Stream = new CompletableQueueStream();
            Result.ContentType = $"text/plain;charset={Encoding.WebName}";
            Result.OnFinally += (o, arg) => TryRemove(Waiter);
            return Result;
        }
        private void SetHeaders(IEnumerable<CompletableStreamResult> Responses, long? ContentLength, string? ContentType, string? ContentDisposition)
        {
            foreach (var r in Responses)
            {
                r.ContentLength = ContentLength;
                r.ContentType = ContentType;
                r.ContentDisposition = ContentDisposition;
            }
        }
        private Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetDataAsync(HttpRequest Request, CancellationToken Token = default)
        {
            foreach (var c in Converters)
                if (c.IsUse(Request.Headers))
                    return c.GetStreamAsync(Request.Headers, Request.Body, Token);
            throw new InvalidOperationException("Empty Stream.");
        }
        private async Task PipingAsync(Stream RequestStream, CompletableQueueStream InfomationStream, IEnumerable<CompletableQueueStream> Buffers, int BufferSize, Encoding Encoding, CancellationToken Token = default)
        {
            using var l = Logger.BeginLogDebugScope(nameof(PipingAsync));
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
        public IActionResult AddReceiver(string RelativeUri, CancellationToken Token = default)
            => AddReceiver(new RequestKey(RelativeUri), Token);
        public IActionResult AddReceiver(RequestKey Key, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            using var l = Logger.BeginLogDebugScope(nameof(AddReceiver));
            var Waiter = Get(Key);
            using var finallyremove = Disposable.Create(() => TryRemove(Waiter));
            Waiter.AssertKey(Key);
            if (!(Waiter.ReceiversCount < Waiter.RequestedReceiversCount))
                throw new InvalidOperationException($"Connection receivers over.");
            var Response = CreateReceiverStreamResult(Waiter);
            Waiter.AddReceiver(Response);
            if (Waiter.IsReady)
                Waiter.ReadyTaskSource.TrySetResult(true);
            return Response;
        }
        private CompletableStreamResult CreateReceiverStreamResult(Pipe Waiter)
        {
            var Result = Services.GetRequiredService<CompletableStreamResult>();
            Result.Identity = "Receiver";
            Result.Stream = new CompletableQueueStream();
            Result.AccessControlAllowOrigin = "*";
            Result.AccessControlExposeHeaders = "Content-Length, Content-Type";
            Result.OnFinally += (o, arg) =>
            {
                var Removed = Waiter.RemoveReceiver(Result);
                Logger.LogDebug("STREAM REMOVE " + (Removed ? "SUCCESS" : "FAILED"));
                TryRemove(Waiter);
            };
            Result.HeaderIsSetCompletedTask = Waiter.HeaderIsSetCompletedTask;
            return Result;
        }
        private async Task SendMessageAsync(Stream Stream, string Message, CancellationToken Token = default)
        {
            Logger.LogDebug(Message);
            await Stream.WriteAsync(Encoding.GetBytes("[INFO] " + Message + Environment.NewLine).AsMemory(), Token);
        }
        private void SendMessage(Stream Stream, string Message)
        {
            Logger.LogDebug(Message);
            Stream.Write(Encoding.GetBytes("[INFO]" + Message + Environment.NewLine).AsSpan());
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
