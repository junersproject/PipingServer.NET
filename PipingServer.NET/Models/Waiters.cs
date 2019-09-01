using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Piping.Streams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace Piping.Models
{
    public class Waiters : IWaiters
    {
        readonly Encoding Encoding;
        readonly IServiceProvider Services;
        readonly ILogger<Waiters> Logger;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Logger"></param>
        public Waiters(IServiceProvider Services, ILogger<Waiters> Logger, Encoding Encoding)
            => (this.Services, this.Logger, this.Encoding) = (Services, Logger, Encoding);
        public IActionResult AddSender(string RelativeUri, HttpRequest Request, CancellationToken Token = default)
            => AddSender(new RequestKey(RelativeUri), Request, Token);
        public IActionResult AddSender(RequestKey Key, HttpRequest Request, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            if (Request.Body.CanSeek)
                Request.Body.Seek(0, SeekOrigin.Begin);
            if (Key.Receivers <= 0)
                throw new InvalidOperationException($"[ERROR] n should > 0, but n = ${Key.Receivers}.\n");
            Logger.LogInformation(string.Join(" ", _waiters.Select(v => $"{ v.Key }:{v.Value}")));

            var w = Get(Key);
            try
            {
                if (w.IsEstablished)
                    throw new InvalidOperationException($"[ERROR] Connection on '{Key.LocalPath}' has been established already.\n");
                string message;
                if (w.RequestedReceiversCount is null)
                    w.RequestedReceiversCount = Key.Receivers;
                else if (Key.Receivers != w.RequestedReceiversCount)
                    throw new InvalidOperationException($"[ERROR] The number of receivers should be ${w.RequestedReceiversCount} but {Key.Receivers}.");
                using var l = Logger.BeginLogInformationScope(nameof(AddSender));
                if (w.IsSetSenderComplete)
                    throw new InvalidOperationException($"[ERROR] The number of receivers should be {w.RequestedReceiversCount} but ${w.ReceiversCount}.\n");
                w.IsSetSenderComplete = true; 
                var Result = Services.GetRequiredService<CompletableStreamResult>();
                Result.Identity = "Sender";
                Result.Stream = new CompletableQueueStream();
                Result.ContentType = $"text/plain;charset={Encoding.WebName}";

                var DataTask = GetDataAsync(Request, Token);
                var ResponseStream = Result.Stream;

                message = $"[INFO] Waiting for {w.RequestedReceiversCount} receiver(s)...";
                Logger.LogInformation(message);
                SendMessage(ResponseStream, Encoding, message);
                message = $"[INFO] {w.ReceiversCount} receiver(s) has/have been connected.";
                Logger.LogInformation(message);
                SendMessage(ResponseStream, Encoding, message);

                _ = Task.Run(async () =>
                {
                    using var l = Logger.BeginLogInformationScope("async " + nameof(AddSender) + " Run");
                    try
                    {
                        if (w.IsReady)
                            w.ReadyTaskSource.TrySetResult(true);
                        else
                            using (Token.Register(() => w.ReadyTaskSource.TrySetCanceled(Token)))
                                await w.ReadyTaskSource.Task;
                        var (Stream, ContentLength, ContentType, ContentDisposition) = await DataTask;
                        message = $"[INFO] {nameof(ContentLength)}:{ContentLength}, {nameof(ContentType)}:{ContentType}, {nameof(ContentDisposition)}:{ContentDisposition}";
                        Logger.LogInformation(message);
                        await SendMessageAsync(ResponseStream, Encoding, message);

                        w.IsEstablished = true;
                        var Buffers = w.Receivers.Select(Response =>
                        {
                            Response.ContentLength = ContentLength;
                            Response.ContentType = ContentType;
                            Response.ContentDisposition = ContentDisposition;
                            return Response.Stream;
                        });
                        message = $"[INFO] Start sending with {w.ReceiversCount} receiver(s)!";
                        Logger.LogInformation(message);
                        await SendMessageAsync(ResponseStream, Encoding, message);

                        var PipingTask = PipingAsync(Stream, ResponseStream, Buffers, 1024, Encoding, Token);
                        w.ResponseTaskSource.TrySetResult(true);
                        await Task.WhenAll(w.ResponseTaskSource.Task, PipingTask);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, nameof(AddSender));
                        w.ResponseTaskSource.TrySetException(e);
                    }
                });
                return Result;
            }
            finally
            {
                TryRemove(w);
            }
        }
        private async ValueTask<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetDataAsync(HttpRequest Request, CancellationToken Token = default)
            => IsMultiForm(Request.Headers) ? await GetPartStreamAsync(Request.Headers, Request.Body, Token) : GetRequestStream(Request);
        private static bool IsMultiForm(IHeaderDictionary Headers)
            => FileUploadSample.MultipartRequestHelper.IsMultipartContentType(Headers["Content-Type"]);
        private async Task PipingAsync(Stream RequestStream, CompletableQueueStream InfomationStream, IEnumerable<CompletableQueueStream> Buffers, int BufferSize, Encoding Encoding, CancellationToken Token = default)
        {
            using var l = Logger.BeginLogInformationScope(nameof(PipingAsync));
            var buffer = new byte[BufferSize];
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            var byteCounter = 0L;
            try
            {
                while ((bytesRead = await RequestStream.ReadAsync(buffer, 0, buffer.Length, Token).ConfigureAwait(false)) != 0)
                {
                    await Stream.WriteAsync(buffer.AsMemory().Slice(0, bytesRead), Token).ConfigureAwait(false);
                    byteCounter += bytesRead;
                }
                var Message = $"[INFO] Sending successful! {byteCounter} bytes.";
                Logger.LogInformation(Message);
                await SendMessageAsync(InfomationStream, Encoding, Message);
                using var writer = new StreamWriter(InfomationStream, Encoding, BufferSize, true);

            }
            finally
            {
                foreach (var b in Buffers)
                    b.CompleteAdding();
                InfomationStream.CompleteAdding();
            }
        }
        /// <summary>
        /// multipart/form-data を
        /// </summary>
        /// <param name="Headers"></param>
        /// <param name="Stream"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        async Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetPartStreamAsync(IHeaderDictionary Headers, Stream Stream, CancellationToken Token = default)
        {
            var enumerable = new AsyncMutiPartFormDataEnumerable(Headers, Stream);
            await foreach (var (headers, stream) in enumerable.WithCancellation(Token))
            {
                var _Stream = stream;
                var ContentLength = headers.ContentLength;
                var ContentType = (string)headers["Content-Type"];
                var ContentDisposition = (string)headers["Content-Disposition"];
                return (_Stream, ContentLength, ContentType, ContentDisposition);
            }
            throw new InvalidOperationException("source is empty");
        }
        (Stream Stream, long? CountentLength, string? ContentType, string? ContentDisposition) GetRequestStream(HttpRequest Request)
            => (
                Request.Body,
                Request.ContentLength,
                Request.ContentType,
                Request.Headers["Content-Disposition"] == StringValues.Empty ? null : string.Join(" ", Request.Headers["Content-Disposition"])
            );
        public IActionResult AddReceiver(string RelativeUri, CancellationToken Token = default)
            => AddReceiver(new RequestKey(RelativeUri), Token);
        public IActionResult AddReceiver(RequestKey Key, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            using var l = Logger.BeginLogInformationScope(nameof(AddReceiver));
            if (Key.Receivers <= 0)
                throw new InvalidOperationException($"[ERROR] n should > 0, but n = {Key.Receivers}.\n");
            var w = Get(Key);
            try
            {
                if (w.IsEstablished)
                    throw new InvalidOperationException($"[ERROR] Connection on '{Key.LocalPath}' has been established already.\n");
                if (w.RequestedReceiversCount is null)
                    w.RequestedReceiversCount = Key.Receivers;
                else if (w.RequestedReceiversCount != Key.Receivers)
                    throw new InvalidOperationException($"[ERROR] The number of receivers should be ${w.RequestedReceiversCount} but {Key.Receivers}.");
                else if (!(w.ReceiversCount < w.RequestedReceiversCount))
                    throw new InvalidOperationException($"[ERROR] Connection receivers over.");
                var Result = Services.GetRequiredService<CompletableStreamResult>();
                Result.Identity = "Receiver";
                Result.Stream = new CompletableQueueStream();
                Result.AccessControlAllowOrigin = "*";
                Result.AccessControlExposeHeaders = "Content-Length, Content-Type";
                Result.OnFinally += (o, arg) =>
                {
                    var Removed = w.RemoveReceiver(Result);
                    Logger.LogInformation("STREAM REMOVE " + (Removed ? "SUCCESS" : "FAILED" ));
                    TryRemove(w);
                };
                w.AddReceiver(Result);
                Result.HeaderIsSetCompletedTask = Task.WhenAll(w.ReadyTaskSource.Task, w.ResponseTaskSource.Task);

                if (w.IsReady)
                    w.ReadyTaskSource.TrySetResult(true);
                return Result;
            }
            finally
            {
                TryRemove(w);
            }
        }
        private async Task SendMessageAsync(Stream Stream, Encoding Encoding, string Message, CancellationToken Token = default)
        {
            var buffer = Encoding.GetBytes(Message + Environment.NewLine).AsMemory();
            await Stream.WriteAsync(buffer, Token);
        }
        private void SendMessage(Stream Stream, Encoding Encoding, string Message)
        {
            Stream.Write(Encoding.GetBytes(Message + Environment.NewLine).AsSpan());
        }
        protected Waiter Get(RequestKey Key)
        {
            lock (_waiters)
            {
                if (_waiters.TryGetValue(Key, out var Waiter))
                {
                    Logger.LogInformation("GET " + Waiter);
                }
                else
                {
                    Waiter = new Waiter(Key);
                    Logger.LogInformation("CREATE " + Waiter);
                    _waiters.Add(Key, Waiter);
                }
                return Waiter;
            }
        }
        protected bool TryRemove(Waiter Waiter)
        {
            lock (_waiters)
            {
                bool Result;
                if (Result = Waiter.IsRemovable)
                {
                    Logger.LogInformation("REMOVE " + Waiter);
                    _waiters.Remove(Waiter.Key);
                }
                else
                {
                    Logger.LogInformation("KEEP " + Waiter);
                }
                return Result;
            }
        }
        protected Dictionary<RequestKey, Waiter> _waiters = new Dictionary<RequestKey, Waiter>();
        protected class Waiter : IDisposable
        {
            public readonly RequestKey Key;
            public Waiter(RequestKey Key)
            {
                this.Key = Key;
            }

            internal readonly TaskCompletionSource<bool> ReadyTaskSource = new TaskCompletionSource<bool>();
            internal readonly TaskCompletionSource<bool> ResponseTaskSource = new TaskCompletionSource<bool>();
            /// <summary>
            /// 待ち合わせが完了しているかどうか
            /// </summary>
            public bool IsEstablished { internal set; get; } = false;
            /// <summary>
            /// Sender が設定済み
            /// </summary>
            public bool IsSetSenderComplete { internal set; get; } = false;
            /// <summary>
            /// Receivers が設定済み
            /// </summary>
            public bool IsSetReceiversComplete => IsEstablished ? true : ReceiversIsAllSet;
            /// <summary>
            /// 削除かのうであるかどうか
            /// </summary>
            public bool IsRemovable
                => (!IsSetSenderComplete && !IsSetReceiversComplete)
                    || (IsSetSenderComplete && IsSetReceiversComplete && _Receivers.Count == 0);
            readonly List<CompletableStreamResult> _Receivers = new List<CompletableStreamResult>();
            public IEnumerable<CompletableStreamResult> Receivers => _Receivers;
            public int ReceiversCount => _Receivers.Count;
            public void AddReceiver(CompletableStreamResult Result) => _Receivers.Add(Result);
            public bool RemoveReceiver(CompletableStreamResult Result) => _Receivers.Remove(Result);
            /// <summary>
            /// Receivers が空
            /// </summary>
            public bool ReceiversIsEmpty => _receiversCount <= 0;
            public bool ReceiversIsAllSet => _Receivers.Count == _receiversCount;
            internal int? _receiversCount = 1;
            /// <summary>
            /// 受け取り数
            /// </summary>
            public int? RequestedReceiversCount
            {
                get => _receiversCount;
                set
                {
                    // 完了してたらNG
                    if (IsEstablished
                        || IsSetSenderComplete
                        || IsSetReceiversComplete)
                        throw new InvalidOperationException("[ERROR] no change " + nameof(RequestedReceiversCount));
                    if (value <= 0)
                        throw new ArgumentException($"{nameof(RequestedReceiversCount)} is 1 or letter.");
                    _receiversCount = value;
                    if (_receiversCount == _Receivers.Count)
                        ReadyTaskSource.TrySetResult(true);
                }
            }
            public bool IsReady => IsSetSenderComplete && ReceiversIsAllSet;
            public override string? ToString()
            {
                return nameof(Waiter) + "{" + string.Join(", ", new[] {
                    nameof(Key) + ":" + Key,
                    nameof(IsEstablished) + ":" + IsEstablished,
                    nameof(IsSetSenderComplete) + ":" + IsSetSenderComplete,
                    nameof(IsSetReceiversComplete) + ":" + IsSetReceiversComplete,
                    nameof(IsRemovable) + ":" + IsRemovable,
                    nameof(ReceiversIsEmpty) + ":" + ReceiversIsEmpty,
                    nameof(RequestedReceiversCount) + ":" + RequestedReceiversCount,
                    nameof(IsReady) + ":" + IsReady,
                    nameof(GetHashCode) + ":" +GetHashCode()
                }.OfType<string>()) + "}";
            }
            #region IDisposable Support
            private bool disposedValue = false; // 重複する呼び出しを検出するには

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        if (!ReadyTaskSource.Task.IsCompleted)
                            ReadyTaskSource.TrySetCanceled();
                        if (!ResponseTaskSource.Task.IsCompleted)
                            ResponseTaskSource.TrySetCanceled();
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
