using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Piping.Server.Core.Properties.Resources;

namespace Piping.Server.Core.Pipes
{
    public sealed class PipingStore : IPipingStore
    {
        readonly ILogger<PipingStore> Logger;
        readonly ILoggerFactory LoggerFactory;
        readonly PipingOptions Options;
        readonly Dictionary<RequestKey, Pipe> _waiters = new Dictionary<RequestKey, Pipe>();
        public PipingStore(ILoggerFactory LoggerFactory, IOptions<PipingOptions> Options)
            => (Logger, this.Options, this.LoggerFactory) = (LoggerFactory.CreateLogger<PipingStore>(), Options.Value, LoggerFactory);
        async Task<IPipe> IPipingStore.GetAsync(RequestKey Key, CancellationToken Token) => await GetAsync(Key, Token);
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
                    Logger.LogDebug(string.Format(PipingStore_Create, Waiter));
                    _waiters.Add(Key, Waiter);
                    Waiter.OnFinally += (o, arg) => RemoveAsync(Waiter);
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

        public Task<bool> RemoveAsync(IPipe Pipe)
        {
            lock (_waiters)
            {
                Logger.LogDebug(string.Format(PipingStore_Remove,Pipe));
                _waiters.Remove(Pipe.Key);
                return Task.FromResult(true);
            }
        }
        public IEnumerator<IPipe> GetEnumerator() => _waiters.Values.OfType<IPipe>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
