using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Piping.Server.Core.Internal;
using static Piping.Server.Core.Properties.Resources;

namespace Piping.Server.Core.Converters
{
    public class MultipartStreamConverter : IStreamConverter
    {
        readonly MultipartStreamConverterOption Option;
        public MultipartStreamConverter(IOptions<MultipartStreamConverterOption> Options) => Option = Options.Value;
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (boundary.IsNullOrWhiteSpace())
                throw new InvalidDataException(MultipartStreamConverter_GetBoundary_IsNullOrWhiteSpace);
            if (boundary.Length > Option.MultipartBoundaryLengthLimit)
                throw new InvalidDataException(
                    string.Format(MultipartStreamConverter_GetBoundary_OverMultipartBoundaryLengthLimit, Option.MultipartBoundaryLengthLimit));
            return boundary.Value;
        }
        const string MultipartMimeTypeStart = "multipart/";
        const string ContentTypeHeaderName = "Content-Type";
        bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf(MultipartMimeTypeStart, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public bool IsUse(IHeaderDictionary Headers) => IsMultipartContentType((Headers ?? throw new ArgumentNullException(nameof(Headers)))[ContentTypeHeaderName]);

        public async Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body == null)
                throw new ArgumentNullException(nameof(Body));
            if (!Body.CanRead)
                throw new ArgumentException(NotReadableStream);
            var ContentType = Headers[ContentTypeHeaderName];
            var boundary = GetBoundary(MediaTypeHeaderValue.Parse((string)ContentType));
            var reader = new MultipartReader(boundary, Body, Option.DefaultBufferSize);
            if ((await reader.ReadNextSectionAsync(Token)) is MultipartSection section)
                return (new HeaderDictionary(section.Headers), section.Body);
            throw new InvalidOperationException(NoDataStream);
        }
    }
}
