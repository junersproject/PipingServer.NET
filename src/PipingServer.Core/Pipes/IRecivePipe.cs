using System.Threading;
using System.Threading.Tasks;

namespace PipingServer.Core.Pipes
{
    public interface IRecivePipe : IPipe
    {
        ValueTask ConnectionAsync(IPipelineStreamResult CompletableStream, CancellationToken Token = default);
    }
}
