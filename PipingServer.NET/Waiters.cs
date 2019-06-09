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

namespace Piping
{
    public class Waiters : IWaiters
    {
        TaskCompletionSource<bool> ReadyTaskSource => new TaskCompletionSource<bool>();
        TaskCompletionSource<bool> ResponseTaskSource => new TaskCompletionSource<bool>();
        public bool IsEstablished { private set; get; } = false;
        public bool IsSetSenderComplete { private set; get; } = false;
        HttpContext? Sender = null;
        List<HttpContext> _Receivers = new List<HttpContext>();
        public bool UnRegisterReceiver(HttpContext Receiver) => _Receivers.Remove(Receiver);
        public bool ReceiversIsEmpty => !_Receivers.Any();
        public IReadOnlyCollection<HttpContext> Receivers { get; }
        int ReceiversCount = 1;
        public Waiters(int ReceiversCount)
            => (this.ReceiversCount, Receivers) = (ReceiversCount, _Receivers.AsReadOnly());
        public bool IsReady() => Sender != null && _Receivers.Count == ReceiversCount;
        public Stream AddSender(RequestKey Key, HttpContext Sender, Encoding Encoding, int BufferSize, CancellationToken Token = default)
        {
            if (IsSetSenderComplete)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be {_Receivers} but ${_Receivers}.\n");
            if (Key.Receivers != ReceiversCount)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be ${ReceiversCount} but {Key.Receivers}.");
            Sender.Response.ContentType = $"text/plain;charset={Encoding.WebName}";
            var ResponseStream = new CompletableQueueStream();
            this.Sender = Sender;
            _ = AddSenderAsync();
            return ResponseStream;
            async Task AddSenderAsync()
            {
                var MultiFormTask = IsMultiForm(Sender.Request.Headers) ? GetPartStreamAsync(Sender, Token) : null;
                Stream Stream;
                long? ContentLength;
                string? ContentType;
                string? ContentDisposition;
                IEnumerable<Stream> Buffers;
                using (var writer = new StreamWriter(ResponseStream, Encoding, BufferSize, true))
                {
                    await writer.WriteLineAsync($"[INFO] Waiting for ${ReceiversCount} receiver(s)...");
                    await writer.WriteLineAsync($"[INFO] {Receivers.Count} receiver(s) has/have been connected.");
                    IsSetSenderComplete = true;
                    if (IsReady())
                        ReadyTaskSource.TrySetResult(true);
                    else
                        await ReadyTaskSource.Task;
                    (Stream, ContentLength, ContentType, ContentDisposition) = MultiFormTask != null ? await MultiFormTask! : GetRequestStream(Sender);
                    await writer.WriteLineAsync($"[INFO] {nameof(ContentLength)}:{ContentLength}, {nameof(ContentType)}:{ContentType}, {nameof(ContentDisposition)}:{ContentDisposition}");
                    IsEstablished = true;
                    Buffers = Receivers.Select(Response =>
                    {
                        var Stream = new CompletableQueueStream();
                        Response.Response.Body = Stream;
                        SetResponse(Response, ContentLength, ContentType, ContentDisposition);
                        return Response.Response.Body;
                    });
                    await writer.WriteLineAsync($"[INFO] Start sending with {Receivers.Count} receiver(s)!");
                }
                _ = PipingAsync(Stream, ResponseStream, Buffers, 1024, Encoding, Token);
                ResponseTaskSource.TrySetResult(true);
            }
        }


        private static bool IsMultiForm(IHeaderDictionary Headers)
            => (Headers["Content-Type"].Any(v => v.ToLower().IndexOf("multipart/form-data") == 0));
        private static async Task PipingAsync(Stream RequestStream, Stream InfomationStream, IEnumerable<Stream> Buffers, int BufferSize, Encoding Encoding, CancellationToken Token = default)
        {
            var buffer = new byte[BufferSize];
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            try
            {
                while ((bytesRead = await RequestStream.ReadAsync(buffer, 0, buffer.Length, Token).ConfigureAwait(false)) != 0)
                    await Stream.WriteAsync(buffer, 0, bytesRead, Token).ConfigureAwait(false);
                using var writer = new StreamWriter(InfomationStream, Encoding, BufferSize, true);
                await writer.WriteLineAsync($"[INFO] Sending successful!");
            }
            finally
            {
                foreach (var b in Buffers)
                    if (b is CompletableQueueStream _b)
                        _b.CompleteAdding();
                if (InfomationStream is CompletableQueueStream __b)
                    __b.CompleteAdding();
            }
        }
        Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetPartStreamAsync(HttpContext Sender, CancellationToken Token = default)
        {
            var tcs = new TaskCompletionSource<(Stream, long?, string?, string?)>();
            var sm = new StreamingMultipartFormDataParser(Sender.Request.Body);
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
        (Stream Stream, long? CountentLength, string? ContentType, string? ContentDisposition) GetRequestStream(HttpContext Sender)
            => (
                Sender.Request.Body,
                Sender.Request.ContentLength,
                Sender.Request.ContentType,
                Sender.Request.Headers["Content-Disposition"] == StringValues.Empty ? null : string.Join(" ", Sender.Request.Headers["Content-Disposition"])
            );
        void SetResponse(HttpContext Response, long? ContentLength, string? ContentType, string? ContentDisposition)
        {
            if (ContentType is string _ContentType)
                Response.Response.ContentType = _ContentType!;
            if (ContentLength is long _ContentLength)
                Response.Response.ContentLength = _ContentLength;
            if (ContentDisposition is string _ContentDisposition)
                Response.Response.Headers["Content-Disposition"] = _ContentDisposition;
        }
        public async Task<Stream> AddReceiverAsync(HttpContext Response, CancellationToken Token = default)
        {
            Response.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Response.Headers.Add("Access-Control-Expose-Headers", "Content-Length, Content-Type");
            _Receivers.Add(Response);
            if (IsReady())
                ReadyTaskSource.TrySetResult(true);
            await ResponseTaskSource.Task;
            return Response.Response.Body;
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
