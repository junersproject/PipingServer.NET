using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.Threading;
using Piping.Server.Core.Internal;
using Piping.Server.Core.Options;
using Piping.Server.Core.Streams;
using static Piping.Server.Core.Properties.Resources;

namespace Piping.Server.Core.Pipes
{
    internal sealed class Pipe : IReadOnlyPipe, IDisposable
    {
        internal PipingOptions Options { get; }
        public RequestKey Key { get; }
        public Pipe(RequestKey Key, PipingOptions Options)
        {
            const string Options_WaitingTimeout = nameof(Options) + "." + nameof(Options.WaitingTimeout);
            this.Key = Key;
            this.Options = Options;
            if (Options.WaitingTimeout < TimeSpan.Zero)
                throw new ArgumentException(string.Format(NameIsValue1RequiredNameIsValue2Over, Options_WaitingTimeout, Options.WaitingTimeout, nameof(TimeSpan.Zero)));
            if (Options.WaitingTimeout is TimeSpan WaitTimeout)
            {
                WaitTokenSource = new CancellationTokenSource(WaitTimeout);
                var Token = WaitTokenSource.Token;
                CancelAction = WaitTokenSource.Token.Register(() =>
                {
                    ReadyTaskSource.TrySetCanceled(Token);
                    ResponseTaskSource.TrySetCanceled(Token);
                });
                ReadyTaskSource.Task.ContinueWith(t => CancelAction?.Dispose());
            }
            RegisterEvents();
        }
        /// <summary>
        /// <see cref="PipingOptions.WaitingTimeout"/> が有効な時に設定されるキャンセル用デリゲート 
        /// </summary>
        readonly IDisposable? CancelAction = null;
        /// <summary>
        /// <see cref="PipingOptions.WaitingTimeout"/> が有効な時に設定されるキャンセル用キャンセルソース
        /// </summary>
        readonly CancellationTokenSource? WaitTokenSource = null;
        /// <summary>
        /// コンストラクタで設定される 各種イベント類
        /// </summary>
        void RegisterEvents()
        {
            // キャンセルイベント
            Task.WhenAll(
                ReadyTaskSource.Task
                , ResponseTaskSource.Task
                , InputDataSource.Task
            ).ContinueWith(
                t => Status = PipeStatus.Canceled
                , TaskContinuationOptions.OnlyOnCanceled);
            // レスポンス開始イベント
            ResponseTaskSource.Task.ContinueWith(
                t => Status = PipeStatus.ResponseStart
                , TaskContinuationOptions.OnlyOnRanToCompletion);
            // 待ち合わせ完了
            ReadyTaskSource.Task.ContinueWith(
                t => Status = PipeStatus.Ready
                , TaskContinuationOptions.OnlyOnRanToCompletion);
        }
        #region Status
        PipeStatus status;
        public PipeStatus Status
        {
            private set
            {
                if (((byte)status) >= (byte)value)
                    return;
                status = value;
                OnStatusChanged?.Invoke(this, new PipeStatusChangedArgs(this));
            }
            get => status;
        }
        #endregion
        #region Header And Stream Set Source
        readonly TaskCompletionSource<(IHeaderDictionary Headers, Stream Stream)> InputDataSource = new TaskCompletionSource<(IHeaderDictionary Headers, Stream Stream)>();
        /// <summary>
        /// Sender が設定済み
        /// </summary>
        internal bool IsSetSenderComplete { private set; get; } = false;
        bool IReadOnlyPipe.IsSetSenderComplete => IsSetSenderComplete;
        /// <summary>
        /// <see cref="PipeStatus.ResponseStart"/>の前までに実施される
        /// </summary>
        /// <param name="DataTask"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        internal async ValueTask SetInputDataAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, CancellationToken Token = default)
        {
            this.DataTask = DataTask;
            if (IsSetSenderComplete)
                throw new PipingException(string.Format(TheNumberOfReceiversShouldBeRequestedReceiversCountButReceiversCount, Key.Receivers, ReceiversCount), this);
            IsSetSenderComplete = true;
            if (Status == PipeStatus.None)
                Status = PipeStatus.Wait;
            await ReadyAsync(Token);
            var Headers = await GetHeadersAsync(Token);
            Receivers.SetHeaders(Headers);
            ResponseTaskSource.TrySetResult(true);
        }
        /// <summary>
        /// ヘッダを取得する
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public async ValueTask<IHeaderDictionary> GetHeadersAsync(CancellationToken Token = default)
        {
            var (Headers, _) = await InputDataSource.Task.ConfigureAwait(false);
            return Headers;
        }
        /// <summary>
        /// 保持用のタスク
        /// </summary>
        Task<(IHeaderDictionary Header, Stream Stream)>? _dataTask = null;
        /// <summary>
        /// ヘッダとストリームのインプット用のタスク
        /// </summary>
        Task<(IHeaderDictionary Header, Stream Stream)>? DataTask
        {
            get
            {
                return _dataTask;
            }
            set
            {
                if (_dataTask != null)
                    throw new PipingException(string.Format(IsAlreadySet, nameof(DataTask)), this);
                _dataTask = value;
                _ = _dataTask?.ContinueWith(SetResult);

            }
        }
        /// <summary>
        /// <see cref="DataTask"/> setter use. callback
        /// </summary>
        /// <param name="DataTask"></param>
        async void SetResult(Task<(IHeaderDictionary Header, Stream Stream)>? DataTask)
        {
            if (!(DataTask is Task<(IHeaderDictionary Header, Stream Stream)> _DataTask))
                throw new ArgumentNullException(nameof(DataTask));
            if (!_DataTask.IsCompleted)
                throw new InvalidOperationException(string.Format(IsNotComplete, nameof(DataTask)));
            if (_DataTask.IsFaulted && _DataTask.Exception is Exception e)
                InputDataSource.TrySetException(e);
            else if (_DataTask.IsCanceled)
                InputDataSource.TrySetCanceled();
            else if (_DataTask.IsCompletedSuccessfully)
                InputDataSource.TrySetResult(await _DataTask);
        }
        #endregion
        #region Ready Source
        readonly TaskCompletionSource<bool> ReadyTaskSource = new TaskCompletionSource<bool>();
        public bool IsReady => IsSetSenderComplete && ReceiversIsAllSet || ReadyTaskSource.Task.IsCompletedSuccessfully;
        public async ValueTask ReadyAsync(CancellationToken Token = default)
        {
            if (ReadyTaskSource.Task.IsCompleted)
                return;
            if (IsReady)
            {
                ReadyTaskSource.TrySetResult(true);
                return;
            }
            await Task.WhenAny(ReadyTaskSource.Task, Token.AsTask());
        }
        #endregion
        #region Response Source
        readonly TaskCompletionSource<bool> ResponseTaskSource = new TaskCompletionSource<bool>();
        public async ValueTask ResponseReady(CancellationToken Token = default)
        {
            if (ResponseTaskSource.Task.IsCompleted)
                return;
            if (InputDataSource.Task.IsCompleted)
                await Task.WhenAny(Task.WhenAll(ReadyTaskSource.Task, ResponseTaskSource.Task), Token.AsTask());
        }
        #endregion
        public bool IsWaitCanceled => ReadyTaskSource.Task.IsCanceled;
        /// <summary>
        /// 待ち合わせが完了しているかどうか
        /// </summary>
        public bool IsEstablished => ReadyTaskSource.Task.IsCompletedSuccessfully;
        /// <summary>
        /// Receivers が設定済み
        /// </summary>
        public bool IsSetReceiversComplete => IsEstablished ? true : ReceiversIsAllSet;
        bool IReadOnlyPipe.IsSetReceiversComplete => IsSetReceiversComplete;
        /// <summary>
        /// 削除かのうであるかどうか
        /// </summary>
        public bool IsRemovable
            => (!IsSetSenderComplete && !IsSetReceiversComplete)
                || (IsSetSenderComplete && IsSetReceiversComplete && Receivers.Count == 0)
                || IsWaitCanceled;
        List<IPipelineStreamResult> Receivers { get; } = new List<IPipelineStreamResult>();
        internal async Task<long> PipingAsync(CancellationToken Token = default)
        {
            var (_, RequestStream) = await InputDataSource.Task;
            return await PipingAsync(RequestStream
                , new byte[Options.BufferSize].AsMemory()
                , Receivers.Select(v => v.Stream)
                , Token);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="RequestStream">入力ストリーム</param>
        /// <param name="Buffer">送信に使用するバッファ</param>
        /// <param name="Buffers"></param>
        /// <param name="Token"></param>
        /// <returns></returns>
        async Task<long> PipingAsync(Stream RequestStream, Memory<byte> Buffer, IEnumerable<PipelineStream> Buffers, CancellationToken Token = default)
        {
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            var byteCounter = 0L;
            using var finallyact = Disposable.Create(() =>
            {
                foreach (var b in Buffers)
                    b.Complete();
            });
            while ((bytesRead = await RequestStream.ReadAsync(Buffer, Token).ConfigureAwait(false)) != 0)
            {
                await Stream.WriteAsync(Buffer.Slice(0, bytesRead), Token).ConfigureAwait(false);
                byteCounter += bytesRead;
            }
            Status = PipeStatus.ResponseEnd;
            return byteCounter;
        }
        /// <summary>
        /// 設定済み受取数
        /// </summary>
        public int ReceiversCount => Receivers.Count;
        /// <summary>
        /// レシーバーの追加
        /// </summary>
        /// <param name="Result"></param>
        internal void AddReceiver(IPipelineStreamResult Result)
        {
            Receivers.Add(Result);
            if (Status == PipeStatus.None)
                Status = PipeStatus.Wait;
            if (Status == PipeStatus.Wait && IsReady && !ReadyTaskSource.Task.IsCompleted)
                ReadyTaskSource.TrySetResult(true);
        }
        /// <summary>
        /// レシーバーの削除
        /// </summary>
        /// <param name="Result"></param>
        /// <returns></returns>
        internal bool RemoveReceiver(IPipelineStreamResult Result) => Receivers.Remove(Result);
        /// <summary>
        /// 受取数に達している
        /// </summary>
        public bool ReceiversIsAllSet => Receivers.Count >= Key.Receivers;
        public override string? ToString()
        {
            return nameof(Pipe) + "{" + string.Join(", ", new[] {
                nameof(Key) + ":" + Key,
                nameof(Status) + ":" + Status,
                nameof(IReadOnlyPipe.Required) + ":" + ((IReadOnlyPipe)this).Required,
                nameof(IsRemovable) + ":" + IsRemovable,
                nameof(ReceiversCount) + ":" + ReceiversCount,
            }.OfType<string>()) + "}";
        }
        public event PipeStatusChangeEventHandler? OnStatusChanged;
        public event EventHandler? OnFinally;
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        internal bool TryRemove()
        {
            bool Removable;
            if (Removable = IsRemovable)
            {
                Dispose();
            }
            return Removable;
        }
        public async IAsyncEnumerable<PipeStatus> OrLaterEventAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var queue = new AsyncQueue<PipeStatus>();
            void Enqueue(object? sender, PipeStatusChangedArgs args) => queue.Enqueue(args.Status);
            OnStatusChanged += Enqueue;
            try
            {
                while (await queue.DequeueAsync(cancellationToken) is PipeStatus Status)
                {
                    yield return Status;
                    if (Status == PipeStatus.Dispose)
                        break;
                }
            }
            finally
            {
                OnStatusChanged -= Enqueue;
            }
        }
        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                if (disposing)
                {
                    if (!ReadyTaskSource.Task.IsCompleted)
                        ReadyTaskSource.TrySetCanceled();
                    if (!ResponseTaskSource.Task.IsCompleted)
                        ResponseTaskSource.TrySetCanceled();
                    if (!InputDataSource.Task.IsCompleted)
                        InputDataSource.TrySetCanceled();
                    Status = PipeStatus.Dispose;
                    foreach (PipeStatusChangeEventHandler e in (OnStatusChanged?.GetInvocationList() ?? Enumerable.Empty<Delegate>()))
                        OnStatusChanged -= e;
                    try
                    {
                        OnFinally?.Invoke(this, new EventArgs());
                    }
                    catch (Exception) { }
                    foreach (EventHandler e in (OnFinally?.GetInvocationList() ?? Enumerable.Empty<Delegate>()))
                        OnFinally -= e;
                    if (WaitTokenSource is CancellationTokenSource TokenSource)
                        TokenSource.Dispose();
                    if (CancelAction is IDisposable Disposable)
                        Disposable.Dispose();
                }
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
