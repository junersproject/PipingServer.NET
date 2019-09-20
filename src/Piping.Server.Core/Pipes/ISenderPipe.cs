using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping.Server.Core.Pipes
{
    public interface ISenderPipe : IPipe
    {
        ValueTask ConnectionAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, IPipelineStreamResult CompletableStream, CancellationToken Token = default);
    }
}
