using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Threading;
using Piping.Server.Core.Options;
using static Piping.Server.Core.Properties.Resources;

namespace Piping.Server.Core.Pipes
{
    public sealed class PipingStore : IPipingStore, IDisposable
    {
        readonly ILogger<PipingStore> Logger;
        readonly ILoggerFactory LoggerFactory;
        readonly PipingOptions Options;
        readonly Dictionary<RequestKey, Pipe> _waiters = new Dictionary<RequestKey, Pipe>();
        public PipingStore(ILoggerFactory LoggerFactory, IOptions<PipingOptions> Options)
            => (Logger, this.Options, this.LoggerFactory) = (LoggerFactory.CreateLogger<PipingStore>(), Options.Value, LoggerFactory);
        public async ValueTask<bool> HasAsync(RequestKey Key, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            lock (_waiters)
            {
                return _waiters.TryGetValue(Key, out _);
            }
        }
        async ValueTask<IReadOnlyPipe?> IPipingStore.GetAsync(RequestKey Key, CancellationToken Token)
        {
            Token.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            return _waiters.TryGetValue(Key, out var Waiter) ? Waiter : null;
        }
        internal async ValueTask<Pipe> GetAsync(RequestKey Key, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            lock (_waiters)
            {
                if (_waiters.TryGetValue(Key, out var Waiter))
                {
                    Logger.LogDebug(string.Format(PipingStore_Get, Waiter));
                }
                else
                {
                    Waiter = new Pipe(Key, Options);
                    Waiter.OnStatusChanged += (p, args) => OnStatusChanged?.Invoke(p, args);
                    Logger.LogDebug(string.Format(PipingStore_Create, Waiter));
                    _waiters.Add(Key, Waiter);
                    Waiter.OnFinally += (o, arg) => RemoveAsync(Key);
                }
                return Waiter;
            }
        }

        public async ValueTask<ISenderPipe> GetSenderAsync(RequestKey Key, CancellationToken Token = default)
        {
            var Pipe = await GetAsync(Key, Token);
            AssertKey(Pipe, Key);
            if (Pipe.IsSetSenderComplete)
                throw new PipingException(ConnectionSenderOver, Pipe);
            return new SenderPipe(Pipe, Options, LoggerFactory.CreateLogger<SenderPipe>());
        }

        public async ValueTask<IRecivePipe> GetReceiveAsync(RequestKey Key, CancellationToken Token = default)
        {
            var Pipe = await GetAsync(Key, Token);
            AssertKey(Pipe, Key);
            if (Pipe.ReceiversCount >= Pipe.Key.Receivers)
                throw new PipingException(ConnectionReceiversOver, Pipe);
            return new RecivePipe(Pipe, LoggerFactory.CreateLogger<RecivePipe>());
        }
        /// <summary>
        /// キーが登録できる状態であるか
        /// </summary>
        /// <param name="Key"></param>
        void AssertKey(Pipe Pipe, RequestKey Key)
        {
            if (Pipe.Status != PipeStatus.None && Pipe.Status != PipeStatus.Wait)
                // 登録できる状態でない
                throw new PipingException(string.Format(ConnectionOnKeyHasBeenEstablishedAlready, Key), Pipe);
            else if (Key.Receivers != Pipe.Key.Receivers)
                // 指定されている受取数に相違がある
                throw new PipingException(string.Format(TheNumberOfReceiversShouldBeRequestedReceiversCountButReceiversCount, Pipe.Key.Receivers, Key.Receivers), Pipe);
        }
        Task<bool> RemoveAsync(RequestKey Key)
        {
            lock (_waiters)
            {
                var pipe = _waiters[Key];
                var reuslt = _waiters.Remove(Key);
                if (pipe is IDisposable disposable)
                    disposable.Dispose();
                if (reuslt)
                    Logger.LogDebug(string.Format(PipingStore_Remove_Success, Key));
                else
                    Logger.LogDebug(string.Format(PipingStore_Remove_Faild, Key));
                return Task.FromResult(reuslt);
            }
        }
        public IEnumerator<IReadOnlyPipe> GetEnumerator() => _waiters.Values.OfType<IReadOnlyPipe>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public event PipeStatusChangeEventHandler? OnStatusChanged;
        public async IAsyncEnumerable<(IReadOnlyPipe Sender, PipeStatus Status)> OrLaterEventAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var queue = new AsyncQueue<(IReadOnlyPipe Sender, PipeStatus Status)>();
            void Enqueue(object? sender, PipeStatusChangedArgs args)
            {
                if (!(sender is IReadOnlyPipe pipe))
                    return;
                queue.Enqueue((pipe, args.Status));
            }
            OnStatusChanged += Enqueue;
            try
            {
                while ((await queue.DequeueAsync(cancellationToken)) is (IReadOnlyPipe Sender, PipeStatus Status) values)
                    yield return values;
            }
            finally
            {
                OnStatusChanged -= Enqueue;
            }
        }
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                if (disposing)
                {
                    foreach (var pipe in _waiters.Values.ToArray())
                        pipe.Dispose();
                    foreach (PipeStatusChangeEventHandler d in (OnStatusChanged?.GetInvocationList() ?? Enumerable.Empty<Delegate>()))
                        OnStatusChanged -= d;
                }
            }
        }

        ~PipingStore() => Dispose(false);

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
