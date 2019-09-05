using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Piping.Converters
{
    /// <summary>
    /// <see cref="Stream"/> と <see cref="IHeaderDictionary"/> を其の儘使用するコンバータ
    /// </summary>
    public class DefaultStreamConverter : IStreamConverter
    {
        public Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            var ContentType = Headers["Content-Type"] is StringValues _ContentType
                ? _ContentType == StringValues.Empty ? null : (string?)_ContentType : throw new InvalidOperationException();
            var ContentDisposition = Headers["Content-Disposition"] is StringValues _ContentDisposition
                ? _ContentDisposition == StringValues.Empty ? null : (string?)_ContentDisposition : throw new InvalidOperationException();
            return Task.FromResult((Body, Headers.ContentLength, ContentType, ContentDisposition));
        }

        public bool IsUse(IHeaderDictionary Headers) => true;
    }
}
