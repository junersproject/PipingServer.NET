using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using HttpMultipartParser;
using System.Net;
using System.Text;
using System.Linq;

#nullable enable
namespace Piping
{
    public class SenderResponseWaiters : IDisposable
    {
        TaskCompletionSource<bool> ReadyTaskSource = new TaskCompletionSource<bool>();
        TaskCompletionSource<bool> ResponseTaskSource = new TaskCompletionSource<bool>();
        public bool IsEstablished { private set; get; } = false;
        public bool IsSetSenderComplete { private set; get; } = false;
        ReqRes? Sender = null;
        List<ReqRes> _Receivers = new List<ReqRes>();
        public bool UnRegisterReceiver(ReqRes Receiver) => _Receivers.Remove(Receiver);
        public bool ReceiversIsEmpty => !_Receivers.Any();
        public IReadOnlyCollection<ReqRes> Receivers { get; }
        int ReceiversCount = 1;
        public SenderResponseWaiters(int ReceiversCount)
            => (this.ReceiversCount, Receivers) = (ReceiversCount, _Receivers.AsReadOnly());
        public bool IsReady() => Sender != null && _Receivers.Count == ReceiversCount;
        public Stream AddSender(RequestKey Key, ReqRes Sender, Encoding Encoding, int BufferSize, CancellationToken Token = default)
        {
            if (IsSetSenderComplete)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be {_Receivers} but ${_Receivers}.\n");
            if (Key.Receivers != ReceiversCount)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be ${ReceiversCount} but {Key.Receivers}.");
            Sender.Context.OutgoingRequest.ContentType = $"text/plain;charset={Encoding.WebName}";
            Sender.ResponseStream = new CompletableQueueStream();
            this.Sender = Sender;
            _ = AddSenderAsync();
            return Sender.ResponseStream;
            async Task AddSenderAsync()
            {
                var MultiFormTask = IsMultiForm(Sender.Context.IncomingRequest.Headers) ? GetPartStreamAsync(Sender, Token) : null;
                Stream? Stream;
                long? ContentLength;
                string? ContentType;
                string? ContentDisposition;
                IEnumerable<Stream> Buffers;
                using (var writer = new StreamWriter(Sender.ResponseStream, Encoding, BufferSize, true))
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
                        SetResponse(Response, ContentLength, ContentType, ContentDisposition);
                        return Response.ResponseStream;
                    });
                    await writer.WriteLineAsync($"[INFO] Start sending with {Receivers.Count} receiver(s)!");
                }
                _ = PipingAsync(Sender, Buffers, 1024, Encoding, Token);
                ResponseTaskSource.TrySetResult(true);
            }
        }

        
        private static bool IsMultiForm(WebHeaderCollection Headers)
            => (Headers[HttpRequestHeader.ContentType] ?? string.Empty).IndexOf("multipart/form-data") == 0;
        private static async Task PipingAsync(ReqRes Sender, IEnumerable<Stream> Buffers, int BufferSize, Encoding Encoding, CancellationToken Token = default)
        {
            var buffer = new byte[BufferSize];
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            try
            {
                while ((bytesRead = await Sender.RequestStream.ReadAsync(buffer, 0, buffer.Length, Token).ConfigureAwait(false)) != 0)
                    await Stream.WriteAsync(buffer, 0, bytesRead, Token).ConfigureAwait(false);
                using var writer = new StreamWriter(Sender.ResponseStream, Encoding, BufferSize, true);
                await writer.WriteLineAsync($"[INFO] Sending successful!");
            }
            finally
            {
                foreach (var b in Buffers)
                    if (b is CompletableQueueStream _b)
                        _b.CompleteAdding();
                if (Sender.ResponseStream is CompletableQueueStream __b)
                    __b.CompleteAdding();
            }
        }
        Task<(Stream Stream, long? ContentLength, string? ContentType, string? ContentDisposition)> GetPartStreamAsync(ReqRes Sender, CancellationToken Token = default)
        {
            var tcs = new TaskCompletionSource<(Stream, long?, string?, string?)>();
            var sm = new StreamingMultipartFormDataParser(Sender.RequestStream);
            sm.FileHandler += (name, fileName, contentType, contentDisposition, buffer, bytes)
                =>
            {
                if (tcs.Task.IsCompleted)
                    return;
                tcs.TrySetResult((new MemoryStream(buffer), buffer.LongLength, contentType, contentDisposition));
            };
            sm.ParameterHandler += (p) => {
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
        (Stream Stream, long? CountentLength, string? ContentType, string? ContentDisposition) GetRequestStream(ReqRes Sender)
            => (
                Sender.RequestStream, 
                long.TryParse(Sender.Context.IncomingRequest.Headers.Get("Content-Length") ?? "", out var ContentLength) ? ContentLength : (long?)null, 
                Sender.Context.IncomingRequest.Headers.Get("Content-Type"), 
                Sender.Context.IncomingRequest.Headers.Get("Content-Disposition"));
        void SetResponse(ReqRes Response, long? ContentLength, string? ContentType, string? ContentDisposition)
        {
            if (ContentType != null)
                Response.Context.OutgoingResponse.ContentType = ContentType!;
            if (ContentLength != null)
                Response.Context.OutgoingResponse.ContentLength = ContentLength.Value;
            if (ContentDisposition != null)
                Response.Context.OutgoingResponse.Headers.Add("Content-Disposition", ContentDisposition!);
        }
        public async Task<Stream> AddReceiverAsync(ReqRes Response, CancellationToken Token = default)
        {
            try
            {
                Response.Context.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Context.OutgoingResponse.Headers.Add("Access-Control-Expose-Headers", "Content-Length, Content-Type");
                Response.ResponseStream = new CompletableQueueStream();
                _Receivers.Add(Response);
                if (IsReady())
                    ReadyTaskSource.TrySetResult(true);
                await ResponseTaskSource.Task;
                return Response.ResponseStream;
            }
            catch (Exception)
            {
                Response.ResponseStream?.Dispose();
                throw;
            }
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
