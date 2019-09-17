using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Piping.Server.Core.Pipes
{
    public interface IPipingStore: IEnumerable<IPipe> {
        Task<ISenderPipe> GetSenderAsync(RequestKey Key, CancellationToken Token = default);
        Task<IRecivePipe> GetReceiveAsync(RequestKey Key, CancellationToken Token = default);
        Task<IPipe> GetAsync(RequestKey Key, CancellationToken Token = default);
        Task<bool> TryRemoveAsync(IPipe Pipe);
    }
}
