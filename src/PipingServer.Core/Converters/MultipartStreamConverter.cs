using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HttpMultipartParser;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PipingServer.Core.Streams;
using static PipingServer.Core.Properties.Resources;

namespace PipingServer.Core.Converters
{
    public class MultipartStreamConverter : IStreamConverter
    {
        readonly MultipartStreamConverterOption Option;
        public MultipartStreamConverter(IOptions<MultipartStreamConverterOption> Options) => Option = Options.Value;
        const string MultipartMimeTypeStart = "multipart/";
        const string ContentTypeHeaderName = "Content-Type";
        const string ContentDispositionHeaderName = "Content-Disposition";
        bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.IndexOf(MultipartMimeTypeStart, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        public bool IsUse(IHeaderDictionary Headers) => IsMultipartContentType((Headers ?? throw new ArgumentNullException(nameof(Headers)))[ContentTypeHeaderName]);

        public Task<(IHeaderDictionary Headers, Stream Stream)> GetStreamAsync(IHeaderDictionary Headers, Stream Body, CancellationToken Token = default)
        {
            if (Headers is null)
                throw new ArgumentNullException(nameof(Headers));
            if (Body is null || Body == System.IO.Stream.Null)
                throw new ArgumentNullException(nameof(Body));
            if (!Body.CanRead)
                throw new ArgumentException(NotReadableStream);
            var source = new TaskCompletionSource<(IHeaderDictionary Header, Stream Stream)>();
            var parser = new StreamingMultipartFormDataParser(Body)
            {
                BinaryBufferSize = Option.BufferSize,
            };
            bool isFirst = true;
            void ParameterHandler(ParameterPart parameter) { }
            var Stream = new PipelineStream();
            void FileHandler(string Name, string FileName, string ContentType, string ContentDisposition, byte[] buffer, int bytes, int partNumber)
            {
                if (isFirst && partNumber == 0)
                {
                    isFirst = false;
                    string DispositionString = string.Empty;
                    if (!string.IsNullOrEmpty(ContentDisposition)) {
                        DispositionString = ContentDisposition;
                        if (!string.IsNullOrEmpty(Name))
                            DispositionString += ";name=" + Name;
                        if (!string.IsNullOrEmpty(FileName))
                            DispositionString += ";filename=" + FileName;
                        Headers[ContentDispositionHeaderName] = DispositionString;
                    }
                    if (!string.IsNullOrEmpty(ContentType))
                    {
                        Headers[ContentTypeHeaderName] = ContentType;
                    }
                    source.TrySetResult((Headers, Stream));

                } else if (partNumber == 0)
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
                } catch (OperationCanceledException e) {
                    source.TrySetCanceled(e.CancellationToken);
                } catch (Exception e)
                {
                    source.TrySetException(e);
                }
            });
            return source.Task;
        }
    }
}
