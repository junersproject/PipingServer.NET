using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Piping.Server.Core.Converters
{
    public static class StreamConverterExtensions
    {
        public static Task<(IHeaderDictionary Headers, Stream Stream)> GetDataAsync(this IEnumerable<IStreamConverter> Converters, HttpRequest Request, CancellationToken Token = default, ILogger? Logger = null)
        {
            foreach (var c in Converters)
                if (!(c is DefaultStreamConverter) && c.IsUse(Request.Headers))
                {
                    Logger?.LogInformation($"USE {c.GetType().FullName}");
                    return c.GetStreamAsync(Request.Headers, Request.Body, Token);
                }
            Logger?.LogInformation($"USE {typeof(DefaultStreamConverter).FullName}");
            return DefaultStreamConverter.GetStreamAsync(Request.Headers, Request.Body, Token);
        }
    }
}
