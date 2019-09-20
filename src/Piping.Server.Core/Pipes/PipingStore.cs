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
        async Task<IReadOnlyPipe> IPipingStore.GetAsync(RequestKey Key, CancellationToken Token) => await GetAsync(Key, Token);
        internal Task<Pipe> GetAsync(RequestKey Key, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
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
                return Task.FromResult(Waiter);
            }
        }

        public async Task<ISenderPipe> GetSenderAsync(RequestKey Key, CancellationToken Token = default)
        {
            var Pipe = await GetAsync(Key, Token);
            Pipe.AssertKey(Key);
            if (Pipe.IsSetSenderComplete)
                throw new PipingException(ConnectionSenderOver, Pipe);
            return new SenderPipe(Pipe, Options, LoggerFactory.CreateLogger<SenderPipe>());
        }

        public async Task<IRecivePipe> GetReceiveAsync(RequestKey Key, CancellationToken Token = default)
        {
            var Pipe = await GetAsync(Key, Token);
            Pipe.AssertKey(Key);
            if (Pipe.ReceiversCount >= Pipe.Key.Receivers)
                throw new PipingException(ConnectionReceiversOver, Pipe);
            return new RecivePipe(Pipe, LoggerFactory.CreateLogger<RecivePipe>());
        }

        public Task<bool> RemoveAsync(RequestKey Key)
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
        public async IAsyncEnumerable<(IReadOnlyPipe Sender, PipeStatus Status)> OrLaterEventAsync([EnumeratorCancellation]CancellationToken cancellationToken = default)
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
