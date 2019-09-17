using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping.Server.Core.Pipes
{
    public interface IReadOnlyPipe : IPipe
    {
        Task<IHeaderDictionary> GetHeadersAsync(CancellationToken Token = default);
    }
}
