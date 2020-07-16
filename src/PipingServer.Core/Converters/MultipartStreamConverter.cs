using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HttpMultipartParser;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using PipingServer.Core.Streams;
using static PipingServer.Core.Properties.Resources;

namespace PipingServer.Core.Converters
{
    public class MultipartStreamConverter : IStreamConverter
    {
        static readonly Encoding Encoding = new UTF8Encoding(false);
        readonly MultipartStreamConverterOption Option;
        public MultipartStreamConverter(IOptions<MultipartStreamConverterOption> Options) => Option = Options.Value;
        const string MultipartMimeTypeStart = "multipart/form-data";
        const string ContentTypeHeaderName = "Content-Type";
        const string ContentDispositionHeaderName = "Content-Disposition";
        const string ParameterMimeType = "text/plain";
        bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf(MultipartMimeTypeStart, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        string? GetBoundary(StringValues contentType)
        {
            var hasBoundary = contentType.FirstOrDefault(text => text.IndexOf("boundary=") >= 0);
            if (hasBoundary is null)
                return null;
            var start = hasBoundary.IndexOf("boundary=") + "boundary=".Length;
            var last = hasBoundary.IndexOf(";", start);
            if (last >= 0)
                hasBoundary = hasBoundary.Substring(start, last - start);
            else
                hasBoundary = hasBoundary.Substring(start);
            return hasBoundary.Trim('"');
        }
        public bool IsUse<IHeaderDictionary>(IDictionary<string, StringValues> Headers)
            where IHeaderDictionary : IDictionary<string, StringValues>
            => (Headers ?? throw new ArgumentNullException(nameof(Headers)))
            .TryGetValue(ContentTypeHeaderName, out var value) && IsMultipartContentType(value);

        public Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync<IHeaderDictionary>(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
            where IHeaderDictionary : IDictionary<string, StringValues>
        {
            if (Headers is null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body is null || Body == System.IO.Stream.Null)
                throw new ArgumentNullException(nameof(Body));
            if (!Body.CanRead)
                throw new ArgumentException(NotReadableStream);
            var source = new TaskCompletionSource<(IHeaderDictionary Header, Stream Stream)>();
            var boundary = Headers.TryGetValue(ContentTypeHeaderName, out var value) ? GetBoundary(value) : null;
            var parser = new StreamingMultipartFormDataParser(Body, boundary, Encoding, Option.BufferSize);
            bool isFirst = true;
            void ParameterHandler(ParameterPart parameter)
            {
                var bytes = Encoding.GetBytes(parameter.Data);
                FileHandler(parameter.Name, string.Empty, ParameterMimeType, string.Empty, bytes, bytes.Length, 0);
            }
            var Stream = new PipelineStream();
            void FileHandler(string Name, string FileName, string ContentType, string ContentDisposition, byte[] buffer, int bytes, int partNumber)
            {
                if (isFirst && partNumber == 0)
                {
                    isFirst = false;
                    var DispositionString = new List<string>();
                    if (!string.IsNullOrEmpty(ContentDisposition))
                    {
                        DispositionString.Add(ContentDisposition);
                        if (!string.IsNullOrEmpty(Name))
                            DispositionString.Add("name=" + Name);
                        if (!string.IsNullOrEmpty(FileName))
                            DispositionString.Add("filename=" + FileName);
                        Headers[ContentDispositionHeaderName] = string.Join(';', DispositionString);
                    }
                    if (!string.IsNullOrEmpty(ContentType))
                    {
                        Headers[ContentTypeHeaderName] = ContentType;
                    }
                    source.TrySetResult((Headers, Stream));

                }
                else if (partNumber == 0)
                {
                    if (!Stream.IsAddingCompleted)
                        Stream.Complete();
                }
                if (Stream.IsAddingCompleted)
                    return;
                Stream.Write(buffer, 0, bytes);

            }
            void StreamCloseHandler()
            {
                if (!Stream.IsAddingCompleted)
                    Stream.Complete();
                Stream.Dispose();
            }
            parser.ParameterHandler += ParameterHandler;
            parser.FileHandler += FileHandler;
            parser.StreamClosedHandler += StreamCloseHandler;
            parser.StreamClosedHandler += () => { };
            _ = Task.Run(async () =>
            {
                try
                {
                    await parser.RunAsync(Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException e)
                {
                    source.TrySetCanceled(e.CancellationToken);
                    Stream.Dispose();
                }
                catch (Exception e)
                {
                    source.TrySetException(e);
                    Stream.Dispose();
                }
                finally
                {
                    source.TrySetCanceled();
                    if (!Stream.IsAddingCompleted)
                        Stream.Complete();
                }
            });
            return source.Task;
        }
    }
}
