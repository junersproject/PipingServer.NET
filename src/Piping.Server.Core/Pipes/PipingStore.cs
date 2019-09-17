using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Piping.Server.Core.Pipes
{
    public sealed class PipingStore : IPipingStore
    {
        readonly ILogger<PipingStore> Logger;
        readonly ILoggerFactory LoggerFactory;
        readonly PipingOptions Options;
        readonly Dictionary<RequestKey, Pipe> _waiters = new Dictionary<RequestKey, Pipe>();
        public PipingStore(ILoggerFactory LoggerFactory, IOptions<PipingOptions> Options)
            => (this.Logger, this.Options, this.LoggerFactory) = (LoggerFactory.CreateLogger<PipingStore>(), Options.Value, LoggerFactory);
        async Task<IPipe> IPipingStore.GetAsync(RequestKey Key, CancellationToken Token) => await GetAsync(Key, Token);
        internal Task<Pipe> GetAsync(RequestKey Key, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
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
                    Waiter.OnFinally += (o, arg) => TryRemoveAsync(Waiter);
                }
                return Task.FromResult(Waiter);
            }
        }

        public async Task<ISenderPipe> GetSenderAsync(RequestKey Key, CancellationToken Token = default)
        {
            var Pipe = await GetAsync(Key, Token);
            Pipe.AssertKey(Key);
            if (Pipe.IsSetSenderComplete)
                throw new InvalidOperationException("Connection sender over.");
            return new SenderPipe(Pipe, Options, LoggerFactory.CreateLogger<ISenderPipe>());
        }

        public async Task<IRecivePipe> GetReceiveAsync(RequestKey Key, CancellationToken Token = default)
        {
            var Pipe = await GetAsync(Key, Token);
            Pipe.AssertKey(Key);
            if (Pipe.ReceiversCount >= Pipe.Key.Receivers)
                throw new InvalidOperationException($"Connection receivers over.");
            return new RecivePipe(Pipe, LoggerFactory.CreateLogger<IRecivePipe>());
        }

        public Task<bool> TryRemoveAsync(IPipe Pipe)
        {
            lock (_waiters)
            {
                bool Result;
                if (Result = Pipe.IsRemovable)
                {
                    Logger.LogDebug("REMOVE " + Pipe);
                    _waiters.Remove(Pipe.Key);
                }
                else
                {
                    Logger.LogDebug("KEEP " + Pipe);
                }
                return Task.FromResult(Result);
            }

        }
        public IEnumerator<IPipe> GetEnumerator() => _waiters.Values.OfType<IPipe>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
