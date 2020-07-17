using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace PipingServer.Mvc.Converters
{
    public interface IStreamConverter
    {
        bool IsUse<IHeaderDictionary>(IDictionary<string, StringValues> Headers) where IHeaderDictionary : IDictionary<string, StringValues>;
        Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync<IHeaderDictionary>(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
            where IHeaderDictionary : IDictionary<string, StringValues>;
    }
}
