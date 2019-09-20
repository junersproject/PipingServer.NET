using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Piping.Server.Core.Pipes
{
    public interface IPipingStore: IEnumerable<IReadOnlyPipe> {
        Task<ISenderPipe> GetSenderAsync(RequestKey Key, CancellationToken Token = default);
        Task<IRecivePipe> GetReceiveAsync(RequestKey Key, CancellationToken Token = default);
        Task<IReadOnlyPipe> GetAsync(RequestKey Key, CancellationToken Token = default);
        IAsyncEnumerable<(IReadOnlyPipe Sender, PipeStatus Status)> OrLaterEventAsync(CancellationToken Token = default);
        event PipeStatusChangeEventHandler? OnStatusChanged;
    }
}
