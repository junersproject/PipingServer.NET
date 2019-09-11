using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Piping.Server.Core.Internal;

namespace Piping.Server.Core.Pipes
{
    internal class Pipe : IPipe, IDisposable
    {
        public RequestKey Key { get; }
        public Pipe(RequestKey Key, PipingOptions Options)
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
        public PipeStatus Status
        {
            get
            {
                if (IsWaitCanceled)
                    return PipeStatus.Canceled;
                if (IsEstablished)
                    return PipeStatus.ResponseStart;
                if (IsReady)
                    return PipeStatus.Ready;
                return PipeStatus.Wait;
            }
        }
        readonly IDisposable? CancelAction = null;
        readonly CancellationTokenSource? WaitTokenSource = null;
        public async ValueTask ResponseReady(CancellationToken Token = default)
        {
            if (ResponseTaskSource.Task.IsCompleted)
                return;
            await Task.WhenAny(Task.WhenAll(ReadyTaskSource.Task, ResponseTaskSource.Task), Token.AsTask());
        }
        readonly TaskCompletionSource<bool> ReadyTaskSource = new TaskCompletionSource<bool>();
        public async ValueTask ReadyAsync(CancellationToken Token = default)
        {
            if (IsReady)
                ReadyTaskSource.TrySetResult(true);
            else
                using (Token.Register(() => ReadyTaskSource.TrySetCanceled(Token)))
                    await ReadyTaskSource.Task;
        }
        readonly TaskCompletionSource<bool> ResponseTaskSource = new TaskCompletionSource<bool>();
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
                throw new InvalidOperationException($"The number of receivers should be {RequestedReceiversCount} but {ReceiversCount}.");
            IsSetSenderComplete = true;
        }
        public async ValueTask SetHeadersAsync(Func<IEnumerable<ICompletableStream>, Task> SetHeaderAction)
        {
            if (!ResponseTaskSource.Task.IsCompleted)
                return;
            try
            {
                await SetHeaderAction.Invoke(Receivers);
                ResponseTaskSource.TrySetResult(true);
            }
            catch(Exception e)
            {
                ResponseTaskSource.TrySetException(e);
                throw;
            }
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
        readonly List<ICompletableStream> _Receivers = new List<ICompletableStream>();
        public IEnumerable<ICompletableStream> Receivers => _Receivers;
        public int ReceiversCount => _Receivers.Count;
        public void AssertKey(RequestKey Key)
        {
            if (IsEstablished)
                throw new InvalidOperationException($"Connection on '{Key.LocalPath}' has been established already.");
            else if (Key.Receivers != RequestedReceiversCount)
                throw new InvalidOperationException($"The number of receivers should be ${RequestedReceiversCount} but {Key.Receivers}.");
        }
        public void AddReceiver(ICompletableStream Result) => _Receivers.Add(Result);
        public bool RemoveReceiver(ICompletableStream Result) => _Receivers.Remove(Result);
        public bool ReceiversIsAllSet => _Receivers.Count == _receiversCount;
        internal int? _receiversCount = null;
        /// <summary>
        /// 受け取り数
        /// </summary>
        public int RequestedReceiversCount => Key.Receivers;
        public bool IsReady => IsSetSenderComplete && ReceiversIsAllSet || IsEstablished;
        public override string? ToString()
        {
            return nameof(Pipe) + "{" + string.Join(", ", new[] {
                nameof(Key) + ":" + Key,
                nameof(Status) + ":" + Status,
                nameof(IsEstablished) + ":" + IsEstablished,
                nameof(IsSetSenderComplete) + ":" + IsSetSenderComplete,
                nameof(IsSetReceiversComplete) + ":" + IsSetReceiversComplete,
                nameof(IsRemovable) + ":" + IsRemovable,
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
}
