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
    public class Waiters : IWaiters
    {
        readonly Encoding Encoding;
        readonly IServiceProvider Services;
        readonly PipingOptions Options;
        readonly ILogger<Waiters> Logger;
        readonly IEnumerable<IStreamConverter> Converters;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Services"></param>
        /// <param name="Logger"></param>
        public Waiters(IServiceProvider Services, ILogger<Waiters> Logger, Encoding Encoding, IEnumerable<IStreamConverter> Converters, IOptions<PipingOptions> Options)
            => (this.Services, this.Logger, this.Encoding, this.Converters, this.Options) = (Services, Logger, Encoding, Converters, Options.Value);
        public IActionResult AddSender(string RelativeUri, HttpRequest Request, CancellationToken Token = default)
            => AddSender(new RequestKey(RelativeUri), Request, Token);
        public IActionResult AddSender(RequestKey Key, HttpRequest Request, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            if (Request.Body.CanSeek)
                Request.Body.Seek(0, SeekOrigin.Begin);
            Logger.LogInformation(string.Join(" ", _waiters.Select(v => $"{ v.Key }:{v.Value}")));

            var w = Get(Key);
            try
            {
                w.AssertKey(Key);
                using var l = Logger.BeginLogInformationScope(nameof(AddSender));
                w.SetSenderComplete();
                var Result = Services.GetRequiredService<CompletableStreamResult>();
                Result.Identity = "Sender";
                Result.Stream = new CompletableQueueStream();
                Result.ContentType = $"text/plain;charset={Encoding.WebName}";
                Result.OnFinally += (o, arg) => TryRemove(w);
                var DataTask = GetDataAsync(Request, Token);
                var ResponseStream = Result.Stream;
                SendMessage(ResponseStream, $"Waiting for {w.RequestedReceiversCount} receiver(s)...");
                SendMessage(ResponseStream, $"{w.ReceiversCount} receiver(s) has/have been connected.");
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
                        var Buffers = w.Receivers.Select(Response =>
                        {
                            Response.ContentLength = ContentLength;
                            Response.ContentType = ContentType;
                            Response.ContentDisposition = ContentDisposition;
                            return Response.Stream;
                        });
                        await SendMessageAsync(ResponseStream, $"Start sending with {w.ReceiversCount} receiver(s)!");
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
        private Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetDataAsync(HttpRequest Request, CancellationToken Token = default)
        {
            foreach (var c in Converters)
                if (c.IsUse(Request.Headers))
                    return c.GetStreamAsync(Request.Headers, Request.Body, Token);
            throw new InvalidOperationException("Empty Stream.");
        }
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
                await SendMessageAsync(InfomationStream, $"Sending successful! {byteCounter} bytes.");
                using var writer = new StreamWriter(InfomationStream, Encoding, BufferSize, true);

            }
            finally
            {
                foreach (var b in Buffers)
                    b.CompleteAdding();
                InfomationStream.CompleteAdding();
            }
        }
        public IActionResult AddReceiver(string RelativeUri, CancellationToken Token = default)
            => AddReceiver(new RequestKey(RelativeUri), Token);
        public IActionResult AddReceiver(RequestKey Key, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            using var l = Logger.BeginLogInformationScope(nameof(AddReceiver));
            var w = Get(Key);
            try
            {
                w.AssertKey(Key);
                if (!(w.ReceiversCount < w.RequestedReceiversCount))
                    throw new InvalidOperationException($"Connection receivers over.");
                var Result = Services.GetRequiredService<CompletableStreamResult>();
                Result.Identity = "Receiver";
                Result.Stream = new CompletableQueueStream();
                Result.AccessControlAllowOrigin = "*";
                Result.AccessControlExposeHeaders = "Content-Length, Content-Type";
                Result.OnFinally += (o, arg) =>
                {
                    var Removed = w.RemoveReceiver(Result);
                    Logger.LogDebug("STREAM REMOVE " + (Removed ? "SUCCESS" : "FAILED"));
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
        protected Waiter Get(RequestKey Key)
        {
            lock (_waiters)
            {
                if (_waiters.TryGetValue(Key, out var Waiter))
                {
                    Logger.LogDebug("GET " + Waiter);
                }
                else
                {
                    Waiter = new Waiter(Key, Options);
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
        protected bool TryRemove(Waiter Waiter)
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
        protected Dictionary<RequestKey, Waiter> _waiters = new Dictionary<RequestKey, Waiter>();
        protected class Waiter : IDisposable
        {
            public readonly RequestKey Key;
            public Waiter(RequestKey Key, PipingOptions Options)
            {
                this.Key = Key;
                if (Options.WatingTimeout < TimeSpan.Zero)
                    throw new ArgumentException($"{nameof(Options)}.{nameof(Options.WatingTimeout)} is {Options.WatingTimeout}. required {nameof(Options.WatingTimeout)} is {nameof(TimeSpan.Zero)} over");
                if (Options.WatingTimeout is TimeSpan WaitTimeout)
                {
                    WaitTokenSource = new CancellationTokenSource(WaitTimeout);
                    var Token = WaitTokenSource.Token;
                    CancelAction = WaitTokenSource.Token.Register(() =>
                    {
                        ReadyTaskSource.TrySetCanceled(Token);
                        ResponseTaskSource.TrySetCanceled(Token);
                    });
                    ReadyTaskSource.Task.ContinueWith(t =>
                    {
                        CancelAction?.Dispose();
                    });
                }
            }
            readonly IDisposable? CancelAction = null;
            readonly CancellationTokenSource? WaitTokenSource = null;
            internal readonly TaskCompletionSource<bool> ReadyTaskSource = new TaskCompletionSource<bool>();
            internal readonly TaskCompletionSource<bool> ResponseTaskSource = new TaskCompletionSource<bool>();
            public bool IsWaitCanceled => ReadyTaskSource.Task.IsCanceled;
            /// <summary>
            /// 待ち合わせが完了しているかどうか
            /// </summary>
            public bool IsEstablished => ReadyTaskSource.Task.IsCompletedSuccessfully;
            /// <summary>
            /// Sender が設定済み
            /// </summary>
            public bool IsSetSenderComplete { private set; get; }
            public void SetSenderComplete()
            {
                if (IsSetSenderComplete)
                    throw new InvalidOperationException($"The number of receivers should be {RequestedReceiversCount} but ${ReceiversCount}.\n");
                IsSetSenderComplete = true;
            }
            /// <summary>
            /// Receivers が設定済み
            /// </summary>
            public bool IsSetReceiversComplete => IsEstablished ? true : ReceiversIsAllSet;
            /// <summary>
            /// 削除かのうであるかどうか
            /// </summary>
            public bool IsRemovable
                => (!IsSetSenderComplete && !IsSetReceiversComplete)
                    || (IsSetSenderComplete && IsSetReceiversComplete && _Receivers.Count == 0)
                    || IsWaitCanceled;
            readonly List<CompletableStreamResult> _Receivers = new List<CompletableStreamResult>();
            public IEnumerable<CompletableStreamResult> Receivers => _Receivers;
            public int ReceiversCount => _Receivers.Count;
            public void AssertKey(RequestKey Key)
            {
                if (IsEstablished)
                    throw new InvalidOperationException($"Connection on '{Key.LocalPath}' has been established already.\n");
                if (RequestedReceiversCount is null)
                    RequestedReceiversCount = Key.Receivers;
                else if (Key.Receivers != RequestedReceiversCount)
                    throw new InvalidOperationException($"The number of receivers should be ${RequestedReceiversCount} but {Key.Receivers}.");
            }
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
            public event EventHandler? OnWaitTimeout;
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
                        foreach (var e in (OnWaitTimeout?.GetInvocationList() ?? Enumerable.Empty<Delegate>()).Cast<EventHandler>())
                            OnWaitTimeout -= e;
                        if (WaitTokenSource is CancellationTokenSource TokenSource)
                            TokenSource.Dispose();
                        if (CancelAction is IDisposable Disposable)
                            Disposable.Dispose();
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
