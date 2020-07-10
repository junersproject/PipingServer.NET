using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using static PipingServer.Core.Properties.Resources;

namespace PipingServer.Core.Converters
{
    public static class StreamConverterExtensions
    {
        public static Task<(IHeaderDictionary Headers, Stream Stream)> GetDataAsync(this IEnumerable<IStreamConverter> Converters, HttpRequest Request, CancellationToken Token = default, ILogger? Logger = null)
        {
            foreach (var c in Converters)
                if (!(c is DefaultStreamConverter) && c.IsUse(Request.Headers))
                {
                    Logger?.LogInformation(string.Format(StreamConverterExtensions_GetDataAsync_UseType, c.GetType().FullName));
                    return c.GetStreamAsync(Request.Headers, Request.Body, Token);
                }
            Logger?.LogInformation(string.Format(StreamConverterExtensions_GetDataAsync_UseType, typeof(DefaultStreamConverter).FullName));
            return DefaultStreamConverter.GetStreamAsync(Request.Headers, Request.Body, Token);
        }
    }
}
