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
        readonly PipingOptions Options;
        public PipingStore(ILogger<PipingStore> Logger, IOptions<PipingOptions> Options)
            => (this.Logger, this.Options) = (Logger, Options.Value);
        private Dictionary<RequestKey, Pipe> _waiters = new Dictionary<RequestKey, Pipe>();
        public Task<IPipe> GetAsync(string Path, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            var Key = new RequestKey(Path);
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
                        TryRemoveAsync(Waiter);
                    };
                }
                return Task.FromResult((IPipe)Waiter);
            }
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
                    if(Pipe is IDisposable Disposable)
                        Disposable.Dispose();
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
