using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PipingServer.Core.Pipes
{
    public interface IPipingStore : IEnumerable<IReadOnlyPipe>
    {
        ValueTask<ISenderPipe> GetSenderAsync(RequestKey Key, CancellationToken Token = default);
        ValueTask<IRecivePipe> GetReceiveAsync(RequestKey Key, CancellationToken Token = default);
        ValueTask<IReadOnlyPipe?> GetAsync(RequestKey Key, CancellationToken Token = default);
        ValueTask<bool> HasAsync(RequestKey Key, CancellationToken Token = default);
        IAsyncEnumerable<(IReadOnlyPipe Sender, PipeStatus Status)> OrLaterEventAsync(CancellationToken Token = default);
        event PipeStatusChangeEventHandler? OnStatusChanged;
    }
}
