using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using static PipingServer.Mvc.Converters.Properties.Resources;
using Microsoft.Net.Http.Headers;

namespace PipingServer.Mvc.Converters
{
    public class MultipartStreamConverter : IStreamConverter
    {
        readonly MultipartStreamConverterOption Option;
        public MultipartStreamConverter(IOptions<MultipartStreamConverterOption> Options) => Option = Options.Value;
        const string MultipartMimeTypeStart = "multipart/form-data";
        const string ContentTypeHeaderName = "Content-Type";
        bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf(MultipartMimeTypeStart, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (StringSegment.IsNullOrEmpty(boundary))
                throw new InvalidDataException(MissingContentTypeBoundary);
            return boundary.Value;
        }
        public bool IsUse<IHeaderDictionary>(IDictionary<string, StringValues> Headers)
            where IHeaderDictionary : IDictionary<string, StringValues>
            => (Headers ?? throw new ArgumentNullException(nameof(Headers)))
            .TryGetValue(ContentTypeHeaderName, out var value) && IsMultipartContentType(value);

        public async Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync<IHeaderDictionary>(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
            where IHeaderDictionary : IDictionary<string, StringValues>
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body == null)
                throw new ArgumentNullException(nameof(Body));
            if (!Body.CanRead)
                throw new ArgumentException(NotReadableStream);
            var ContentType = Headers[ContentTypeHeaderName];
            var boundary = GetBoundary(MediaTypeHeaderValue.Parse((string)ContentType));
            var reader = new MultipartReader(boundary, Body, Option.BufferSize)
            {
                BodyLengthLimit = Option.BodyLengthLimit,
                HeadersCountLimit = Option.HeadersCountLimit,
                HeadersLengthLimit = Option.HeadersLengthLimit,
            };
            if ((await reader.ReadNextSectionAsync(Token)) is MultipartSection section)
            {
                Headers.Clear();
                foreach (var h in section.Headers)
                    Headers.Add(h.Key, h.Value);
                return (Headers, section.Body);
            }
            throw new InvalidOperationException(NoDataStream);
        }
    }
}
