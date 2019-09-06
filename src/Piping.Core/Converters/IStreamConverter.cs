using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping.Core.Converters
{
    public interface IStreamConverter
    {
        bool IsUse(IHeaderDictionary Headers);
        Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default);
    }
}
