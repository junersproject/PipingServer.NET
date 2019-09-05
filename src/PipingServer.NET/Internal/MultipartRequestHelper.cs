using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
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
                throw new ArgumentException("Missing content-type boundary.");
            if (boundary.Length > MultipartBoundaryLengthLimit)
                throw new ArgumentException(
                    $"Multipart boundary length limit {MultipartBoundaryLengthLimit} exceeded.");
            return boundary.Value;
        }

        public static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

}
