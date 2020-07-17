using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using static PipingServer.Mvc.Converters.Properties.Resources;

namespace PipingServer.Mvc.Converters
{
    public static class StreamConverterExtensions
    {
        public static Task<(IHeaderDictionary Headers, Stream Stream)> GetDataAsync(this IEnumerable<IStreamConverter> Converters, IFeatureCollection Features, CancellationToken Token = default, ILogger? Logger = null)
            => GetDataAsync(Converters, Features.Get<IHttpRequestFeature>(), Token, Logger);

        public static Task<(IHeaderDictionary Headers, Stream Stream)> GetDataAsync(this IEnumerable<IStreamConverter> Converters, IHttpRequestFeature Request, CancellationToken Token = default, ILogger? Logger = null)
            => GetDataAsync(Converters, Request.Headers, Request.Body, Token, Logger);
        public static Task<(T Headers, Stream Stream)> GetDataAsync<T>(this IEnumerable<IStreamConverter> Converters, T Headers, Stream Body, CancellationToken Token = default, ILogger? Logger = null)
            where T : IDictionary<string, StringValues>
        {
            foreach (var c in Converters)
                if (c is IStreamConverter converter
                    && !(converter is DefaultStreamConverter)
                    && converter.IsUse<T>(Headers))
                {
                    Logger?.LogInformation(string.Format(StreamConverterExtensions_GetDataAsync_UseType, c.GetType().FullName));
                    return converter.GetStreamAsync<T>(Headers, Body, Token);
                }
            Logger?.LogInformation(string.Format(StreamConverterExtensions_GetDataAsync_UseType, typeof(DefaultStreamConverter).FullName));
            return DefaultStreamConverter.GetStreamAsync<T>(Headers, Body, Token);
        }
    }
}
