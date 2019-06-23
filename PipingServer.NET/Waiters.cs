using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Http;
using HttpMultipartParser;
using Microsoft.Extensions.Primitives;
using Piping.Streams;
using Microsoft.Extensions.Logging;

namespace Piping
{
    public class Waiters : IWaiters
    {
        readonly ILoggerFactory loggerFactory;
        readonly ILogger<Waiters> logger;
        TaskCompletionSource<bool> ReadyTaskSource => new TaskCompletionSource<bool>();
        TaskCompletionSource<bool> ResponseTaskSource => new TaskCompletionSource<bool>();
        public bool IsEstablished { private set; get; } = false;
        public bool IsSetSenderComplete { private set; get; } = false;
        List<CompletableStreamResult> Receivers = new List<CompletableStreamResult>();
        public bool ReceiversIsEmpty => ReceiversCount <= 0;
        int ReceiversCount = 1;
        public void DecrementReceivers() => ReceiversCount--;
        public Waiters(ILoggerFactory loggerFactory)
            => (this.loggerFactory, logger) = (loggerFactory, loggerFactory.CreateLogger<Waiters>());
        public Waiters(int ReceiversCount, ILoggerFactory loggerFactory)
            => (this.ReceiversCount, this.loggerFactory, this.logger) = (ReceiversCount, loggerFactory, loggerFactory.CreateLogger<Waiters>());
        public bool IsReady() => IsSetSenderComplete && Receivers.Count == ReceiversCount;
        public async Task<CompletableStreamResult> AddSenderAsync(RequestKey Key, HttpRequest Request, HttpResponse Response, Encoding Encoding, int BufferSize, CancellationToken Token = default)
        {
            string message;
            using var l = logger.BeginLogInformationScope(nameof(AddSenderAsync));
            if (IsSetSenderComplete)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be {ReceiversCount} but ${Receivers.Count}.\n");
            if (Key.Receivers != ReceiversCount)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be ${ReceiversCount} but {Key.Receivers}.");
            var Result = new CompletableStreamResult(loggerFactory.CreateLogger<CompletableStreamResult>())
            {
                Stream = new CompletableQueueStream(),
                ContentType = $"text/plain;charset={Encoding.WebName}",
            };
            IsSetSenderComplete = true;
            var DataTask = GetDataAsync(Request, Encoding, BufferSize, Token);
            var ResponseStream = Result.Stream;

            message = $"[INFO] Waiting for {ReceiversCount} receiver(s)...";
            logger.LogInformation(message);
            await SendMessageAsync(ResponseStream, Encoding, BufferSize, message);
            message = $"[INFO] {Receivers.Count} receiver(s) has/have been connected.";
            logger.LogInformation(message);
            await SendMessageAsync(ResponseStream, Encoding, BufferSize, message);
            
            _ = Task.Run(async () =>
            {
                using var l = logger.BeginLogInformationScope("async " + nameof(AddSenderAsync) + " Run");
                try
                {
                    if (IsReady())
                        ReadyTaskSource.TrySetResult(true);
                    else
                        using (Token.Register(() => ReadyTaskSource.TrySetCanceled(Token)))
                            await ReadyTaskSource.Task;
                    var (Stream, ContentLength, ContentType, ContentDisposition) = await DataTask;
                    message = $"[INFO] {nameof(ContentLength)}:{ContentLength}, {nameof(ContentType)}:{ContentType}, {nameof(ContentDisposition)}:{ContentDisposition}";
                    logger.LogInformation(message);
                    await SendMessageAsync(ResponseStream, Encoding, BufferSize, message);

                    IsEstablished = true;
                    var Buffers = Receivers.Select(Response =>
                    {
                        Response.ContentLength = ContentLength;
                        Response.ContentType = ContentType;
                        Response.ContentDisposition = ContentDisposition;
                        return Response.Stream;
                    });
                    message = $"[INFO] Start sending with {Receivers.Count} receiver(s)!";
                    logger.LogInformation(message);
                    await SendMessageAsync(ResponseStream, Encoding, BufferSize, message);

                    var PipingTask = PipingAsync(Stream, ResponseStream, Buffers, 1024, Encoding, Token);
                    ResponseTaskSource.TrySetResult(true);
                    await PipingTask;
                }
                catch (Exception e)
                {
                    logger.LogError(e,nameof(AddSenderAsync));
                    ResponseTaskSource.TrySetException(e);
                }
                finally
                {
                    Receivers.Clear();
                }
            });
            return Result;
        }
        private async ValueTask<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetDataAsync(HttpRequest Request, Encoding Encoding, int BufferSize, CancellationToken Token = default)
            => IsMultiForm(Request.Headers) ? await GetPartStreamAsync(Request.Body, Encoding, BufferSize, Token) : GetRequestStream(Request);
        private static bool IsMultiForm(IHeaderDictionary Headers)
            => (Headers["Content-Type"].Any(v => v.ToLower().IndexOf("multipart/form-data") == 0));
        private async Task PipingAsync(Stream RequestStream, CompletableQueueStream InfomationStream, IEnumerable<CompletableQueueStream> Buffers, int BufferSize, Encoding Encoding, CancellationToken Token = default)
        {
            using var l = logger.BeginLogInformationScope(nameof(PipingAsync));
            var buffer = new byte[BufferSize];
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            try
            {
                while ((bytesRead = await RequestStream.ReadAsync(buffer, 0, buffer.Length, Token).ConfigureAwait(false)) != 0)
                    await Stream.WriteAsync(buffer, 0, bytesRead, Token).ConfigureAwait(false);
                var Message = "[INFO] Sending successful!";
                logger.LogInformation(Message);
                await SendMessageAsync(InfomationStream, Encoding, BufferSize, Message);
                using var writer = new StreamWriter(InfomationStream, Encoding, BufferSize, true);
                
            }
            finally
            {
                foreach (var b in Buffers)
                    b.CompleteAdding();
                InfomationStream.CompleteAdding();
            }
        }
        Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetPartStreamAsync(Stream Stream, Encoding Encoding, int bufferSize, CancellationToken Token = default)
        {
            var tcs = new TaskCompletionSource<(Stream, long?, string?, string?)>();
            var sm = new StreamingMultipartFormDataParser(Stream, Encoding, bufferSize);
            sm.FileHandler += (name, fileName, contentType, contentDisposition, buffer, bytes)
                =>
            {
                if (tcs.Task.IsCompleted)
                    return;
                var _ContentDisposition = string.IsNullOrEmpty(fileName) ? null : $"{contentDisposition};filename='{fileName.Replace("'", "\\'")}';filename*=utf-8''{System.Web.HttpUtility.UrlEncode(fileName).Replace("+", "%20")}";
                tcs.TrySetResult((new MemoryStream(buffer, 0, bytes), buffer.LongLength, contentType, _ContentDisposition));
            };
            sm.ParameterHandler += (p) =>
            {
                if (tcs.Task.IsCompleted)
                    return;
                byte[] bytes = sm.Encoding.GetBytes(p.Data);
                tcs.TrySetResult((new MemoryStream(bytes), bytes.LongLength, $"text/plain; charset={sm.Encoding.WebName}", null));
            };
            sm.StreamClosedHandler += () =>
            {
                tcs.TrySetResult((new MemoryStream(new byte[0]), 0, null, null));
            };
            sm.Run();
            return tcs.Task;
        }
        (Stream Stream, long? CountentLength, string? ContentType, string? ContentDisposition) GetRequestStream(HttpRequest Request)
            => (
                Request.Body,
                Request.ContentLength,
                Request.ContentType,
                Request.Headers["Content-Disposition"] == StringValues.Empty ? null : string.Join(" ", Request.Headers["Content-Disposition"])
            );
        public async Task<CompletableStreamResult> AddReceiverAsync(HttpResponse Response, CancellationToken Token = default)
        {
            using var l = logger.BeginLogInformationScope(nameof(AddReceiverAsync));
            try
            {
                var Result = new CompletableStreamResult(loggerFactory.CreateLogger<CompletableStreamResult>())
                {
                    Stream = new CompletableQueueStream(),
                    AccessControlAllowOrigin = "*",
                    AccessControlExposeHeaders = "Content-Length, Content-Type",
                };
                Receivers.Add(Result);
                if (IsReady())
                    ReadyTaskSource.TrySetResult(true);
                await ResponseTaskSource.Task;
                return Result;
            }
            catch (Exception)
            {
                DecrementReceivers();
                throw;
            }
        }
        private async Task SendMessageAsync(Stream Stream, Encoding Encoding, int BufferSize, string Message, CancellationToken Token = default)
        {
            using var writer = new StreamWriter(Stream, Encoding, BufferSize, true);
            await writer.WriteLineAsync(Message.AsMemory(), Token);
        }
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!ReadyTaskSource.Task.IsCompleted)
                        ReadyTaskSource.TrySetCanceled();
                    if (!ResponseTaskSource.Task.IsCompleted)
                        ResponseTaskSource.TrySetCanceled();
                }
                disposedValue = true;
            }
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
