using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping.Server.Core.Pipes
{
    public interface IReadOnlyPipe : IPipe
    {
        PipeType Required { get; }
        ValueTask<IHeaderDictionary> GetHeadersAsync(CancellationToken Token = default);
        IAsyncEnumerable<PipeStatus> OrLaterEventAsync(CancellationToken Token = default);
        event PipeStatusChangeEventHandler? OnStatusChanged;
    }
}
