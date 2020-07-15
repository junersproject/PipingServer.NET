using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PipingServer.Core.Converters
{
    public interface IStreamConverter
    {
        bool IsUse(IHeaderDictionary Headers);
        Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default);
    }
}
