using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Piping.Internal;

namespace FileUploadSample
{
    /// <summary>
    /// base is https://github.com/aspnet/AspNetCore.Docs/blob/master/aspnetcore/mvc/models/file-uploads/sample/FileUploadSample/MultipartRequestHelper.cs
    /// </summary>
    internal static class MultipartRequestHelper
    {
        public const int MultipartBoundaryLengthLimit = 1024;
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec says 70 characters is a reasonable limit.
        public static string GetBoundary(MediaTypeHeaderValue contentType, int MultipartBoundaryLengthLimit = MultipartBoundaryLengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary);
            if (boundary.IsNullOrWhiteSpace())
                throw new InvalidDataException("Missing content-type boundary.");
            if (boundary.Length > MultipartBoundaryLengthLimit)
                throw new InvalidDataException(
                    $"Multipart boundary length limit {MultipartBoundaryLengthLimit} exceeded.");
            return boundary.Value;
        }

        public static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="key";
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && contentDisposition.FileName.IsNullOrEmpty()
                   && contentDisposition.FileNameStar.IsNullOrEmpty();
        }

        public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                   && contentDisposition.DispositionType.Equals("form-data")
                   && (!contentDisposition.FileName.IsNullOrEmpty()
                       || !contentDisposition.FileNameStar.IsNullOrEmpty());
        }
        public static async IAsyncEnumerable<MultipartSection> GetMutipartSectionAsync(IHeaderDictionary Headers, Stream Stream, int MultipartBoundaryLengthLimit = MultipartBoundaryLengthLimit, [EnumeratorCancellation]CancellationToken Token = default)
        {
            if (Headers == null)
                throw new ArgumentNullException(nameof(Headers));
            if (Stream == null || Stream == Stream.Null)
                throw new ArgumentNullException(nameof(Stream));
            if (!Stream.CanRead)
                yield break;
            var ContentType = Headers["Content-Type"];
            if (!IsMultipartContentType(ContentType))
                throw new ArgumentException($"Expected a multipart request, but got {ContentType}");
            
            var boundary = GetBoundary(
                MediaTypeHeaderValue.Parse((string)ContentType),
                MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, Stream);
            while ((await reader.ReadNextSectionAsync(Token)) is MultipartSection section)
                yield return section;
        }
    }

}
