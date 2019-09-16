using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Piping.Server.Core.Internal;

namespace Piping.Server.Core.Pipes
{
    internal sealed class Pipe : IReadOnlyPipe, IDisposable
    {
        internal PipingOptions Options { get; }
        public RequestKey Key { get; }
        public Pipe(RequestKey Key, PipingOptions Options)
        {
            this.Key = Key;
            this.Options = Options;
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
        readonly TaskCompletionSource<IHeaderDictionary> HeaderSetSource = new TaskCompletionSource<IHeaderDictionary>();
        public Task<IHeaderDictionary> GetHeadersAsync(CancellationToken Token = default) => HeaderSetSource.Task;
        Task<(IHeaderDictionary Header, Stream Stream)>? _dataTask = null;
        internal Task<(IHeaderDictionary Header, Stream Stream)>? DataTask {
            get
            {
                return _dataTask;
            }
            set
            {
                if (_dataTask != null)
                    throw new InvalidOperationException();
                _dataTask = value;
                _ = _dataTask?.ContinueWith(SetResult);
            }
        }
        private async void SetResult(Task<(IHeaderDictionary Header, Stream Stream)>? DataTask)
        {
            if (!(DataTask is Task<(IHeaderDictionary Header, Stream Stream)> _DataTask))
                throw new ArgumentNullException(nameof(DataTask));
            if (!_DataTask.IsCompleted)
                throw new InvalidOperationException("DataTask is not complete.");
            if (_DataTask.IsFaulted && _DataTask.Exception is Exception e)
                HeaderSetSource.TrySetException(e);
            else if (_DataTask.IsCanceled)
                HeaderSetSource.TrySetCanceled();
            else if (_DataTask.IsCompletedSuccessfully)
            {
                var (Header, _) = await _DataTask;
                HeaderSetSource.TrySetResult(Header);
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
        public bool IsReady => IsSetSenderComplete && ReceiversIsAllSet || IsEstablished;
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
        bool IsSetSenderComplete { set; get; }
        private void SetSenderComplete()
        {
            if (IsSetSenderComplete)
                throw new InvalidOperationException($"The number of receivers should be {RequestedReceiversCount} but {ReceiversCount}.");
            IsSetSenderComplete = true;
        }
        internal async ValueTask SetHeadersAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, CancellationToken Token = default)
        {
            this.DataTask = DataTask;
            SetSenderComplete();
            await ReadyAsync();
            var Headers = await GetHeadersAsync();
            Receivers.SetHeaders(Headers);
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
                || (IsSetSenderComplete && IsSetReceiversComplete && Receivers.Count == 0)
                || IsWaitCanceled;
        readonly List<ICompletableStream> Receivers = new List<ICompletableStream>();
        /// <summary>
        /// 設定済み受取数
        /// </summary>
        public int ReceiversCount => Receivers.Count;
        /// <summary>
        /// キーが登録できる状態であるか
        /// </summary>
        /// <param name="Key"></param>
        public void AssertKey(RequestKey Key)
        {
            if (IsEstablished)
                // 登録できる状態でない
                throw new InvalidOperationException($"Connection on '{Key}' has been established already.");
            else if (Key.Receivers != RequestedReceiversCount)
                // 指定されている受取数に相違がある
                throw new InvalidOperationException($"The number of receivers should be ${RequestedReceiversCount} but {Key.Receivers}.");
        }
        /// <summary>
        /// レシーバーの追加
        /// </summary>
        /// <param name="Result"></param>
        public void AddReceiver(ICompletableStream Result)
        {
            Receivers.Add(Result);
            if (IsReady)
                ReadyTaskSource.TrySetResult(true);
        }
        /// <summary>
        /// レシーバーの削除
        /// </summary>
        /// <param name="Result"></param>
        /// <returns></returns>
        public bool RemoveReceiver(ICompletableStream Result) => Receivers.Remove(Result);
        /// <summary>
        /// 受取数に達している
        /// </summary>
        public bool ReceiversIsAllSet => Receivers.Count >= Key.Receivers;
        /// <summary>
        /// 受取数
        /// </summary>
        public int RequestedReceiversCount => Key.Receivers;
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
                nameof(GetHashCode) + ":" +GetHashCode()
            }.OfType<string>()) + "}";
        }
        public event EventHandler? OnWaitTimeout;
        public event PipeStatusChangeEventHandler? OnStatusChanged;
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!ReadyTaskSource.Task.IsCompleted)
                        ReadyTaskSource.TrySetCanceled();
                    if (!ResponseTaskSource.Task.IsCompleted)
                        ResponseTaskSource.TrySetCanceled();
                    if (!HeaderSetSource.Task.IsCompleted)
                        HeaderSetSource.TrySetCanceled();
                    foreach (var e in (OnWaitTimeout?.GetInvocationList() ?? Enumerable.Empty<Delegate>()).Cast<EventHandler>())
                        OnWaitTimeout -= e;
                    foreach (var e in (OnStatusChanged?.GetInvocationList() ?? Enumerable.Empty<Delegate>()).Cast<PipeStatusChangeEventHandler>())
                        OnStatusChanged -= e;
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
