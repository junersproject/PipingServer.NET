using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using Piping.Internal;

namespace Piping.Converters
{
    public class MultipartStreamConverter : IStreamConverter
    {
        public const int MultipartBoundaryLengthLimit = 1024;
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        static string GetBoundary(MediaTypeHeaderValue contentType, int MultipartBoundaryLengthLimit = MultipartBoundaryLengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (boundary.IsNullOrWhiteSpace())
                throw new InvalidDataException("Missing content-type boundary.");
            if (boundary.Length > MultipartBoundaryLengthLimit)
                throw new InvalidDataException(
                    $"Multipart boundary length limit {MultipartBoundaryLengthLimit} exceeded.");
            return boundary.Value;
        }

        static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public bool IsUse(IHeaderDictionary Headers) => IsMultipartContentType((Headers ?? throw new ArgumentNullException(nameof(Headers)))["Content-Type"]);

        public async Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body == null)
                throw new ArgumentNullException(nameof(Body));
            if (!Body.CanRead)
                throw new ArgumentException("Not Readable Stream.");
            var ContentType = Headers["Content-Type"];
            var boundary = GetBoundary(
                MediaTypeHeaderValue.Parse((string)ContentType),
                MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, Body);
            if ((await reader.ReadNextSectionAsync(Token)) is MultipartSection section)
            {
                var _Headers = new HeaderDictionary(section.Headers);
                return (section.Body, _Headers.ContentLength, section.ContentType, section.ContentDisposition);
            }
            throw new InvalidOperationException("No Data Stream.");
        }
    }
}
