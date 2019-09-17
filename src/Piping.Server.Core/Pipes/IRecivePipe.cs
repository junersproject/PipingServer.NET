using System.Threading;
using System.Threading.Tasks;

namespace Piping.Server.Core.Pipes
{
    public interface IRecivePipe : IPipe
    {
        ValueTask ConnectionAsync(ICompletableStream CompletableStream, CancellationToken Token = default);
    }
}
