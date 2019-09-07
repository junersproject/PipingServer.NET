using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Piping.Core.Internal;

namespace Piping.Core.Converters
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
                throw new InvalidDataException("Missing content-type boundary.");
            if (boundary.Length > Option.MultipartBoundaryLengthLimit)
                throw new InvalidDataException(
                    $"Multipart boundary length limit {Option.MultipartBoundaryLengthLimit} exceeded.");
            return boundary.Value;
        }

        bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public bool IsUse(IHeaderDictionary Headers) => IsMultipartContentType((Headers ?? throw new ArgumentNullException(nameof(Headers)))["Content-Type"]);

        public async Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body == null)
                throw new ArgumentNullException(nameof(Body));
            if (!Body.CanRead)
                throw new ArgumentException("Not Readable Stream.");
            var ContentType = Headers["Content-Type"];
            var boundary = GetBoundary(MediaTypeHeaderValue.Parse((string)ContentType));
            var reader = new MultipartReader(boundary, Body, Option.DefaultBufferSize);
            if ((await reader.ReadNextSectionAsync(Token)) is MultipartSection section)
                return (new HeaderDictionary(section.Headers), section.Body);
            throw new InvalidOperationException("No Data Stream.");
        }
    }
}
