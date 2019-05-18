using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.ServiceModel.Web;
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
        TaskCompletionSource<bool> ResponseTasksource = new TaskCompletionSource<bool>();
        Task<bool> ReadyTask => ReadyTaskSource.Task;
        Task<bool> ResponseTask => ResponseTasksource.Task;
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
        public async Task<Stream> AddSenderAsync(RequestKey Key, ReqRes Sender, Encoding Encoding, int BufferSize, CancellationToken Token = default)
        {
            if (IsSetSenderComplete)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be {_Receivers} but ${_Receivers}.\n");
            if (Key.Receivers != ReceiversCount)
                throw new InvalidOperationException($"[ERROR] The number of receivers should be ${ReceiversCount} but {Key.Receivers}.");
            Sender.Response.ContentType = $"text/plain;charset={Encoding.WebName}";
            using (var writer = new StreamWriter(Sender.ResponseStream, Encoding, BufferSize, true))
            {
                await writer.WriteLineAsync($"[INFO] Waiting for ${Receivers} receiver(s)...");
                await writer.WriteLineAsync($"[INFO] {Receivers.Count} receiver(s) has/have been connected.");
            }
            this.Sender = Sender;
            IsSetSenderComplete = true;
            var IsMultiForm = (Sender.Request.Headers[HttpResponseHeader.ContentType] ?? "").IndexOf("multipart/form-data") > 0;
            var MultiFormTask = IsMultiForm ? GetPartStreamAsync(Sender, Token) : null;
            var (Stream, ContentLength, ContentType, ContentDisposition) = IsMultiForm ? await MultiFormTask! : GetRequestStream(Sender);
            if (IsReady())
                ReadyTaskSource.TrySetResult(true);
            else
                await ReadyTask;
            IsEstablished = true;
            var Buffers = new List<BufferStream>();
            foreach (var Response in _Receivers)
            {
                SetResponse(Response, ContentLength, ContentType, ContentDisposition);
                var Buffer = new BufferStream();
                Response.ResponseStream = Buffer;
                Buffers.Add(Buffer);
            }
            await PipingAsync(Stream, Buffers.ToArray(), 1024, Token);
            return Sender.ResponseStream;
        }
        private static async Task PipingAsync(Stream RequestStream, IEnumerable<BufferStream> Buffers, int BufferSize, CancellationToken Token = default)
        {
            var buffer = new byte[BufferSize];
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            try
            {
                while ((bytesRead = await RequestStream.ReadAsync(buffer, 0, buffer.Length, Token).ConfigureAwait(false)) != 0)
                    await Stream.WriteAsync(buffer, 0, bytesRead, Token).ConfigureAwait(false);
            }
            finally
            {
                foreach (var b in Buffers)
                    b.CompleteAdding();
            }
        }
        Task<(Stream Stream, long ContentLength, string ContentType, string ContentDisposition)> GetPartStreamAsync(ReqRes Sender, CancellationToken Token = default)
        {
            var tcs = new TaskCompletionSource<(Stream, long, string, string)>();
            var sm = new StreamingMultipartFormDataParser(Sender.RequestStream);
            sm.FileHandler += (name, fileName, contentType, contentDisposition, buffer, bytes)
                => tcs.TrySetResult((new MemoryStream(buffer), buffer.LongLength, contentType, contentDisposition));
            sm.Run();
            return tcs.Task;
        }
        (Stream Stream, long? CountentLength, string? ContentType, string? ContentDisposition) GetRequestStream(ReqRes Sender)
            => (
                Sender.RequestStream, 
                long.TryParse(Sender.Request.Headers.Get("Content-Length") ?? "", out var ContentLength) ? ContentLength : (long?)null, 
                Sender.Request.Headers.Get("Content-Type"), 
                Sender.Request.Headers.Get("Content-Disposition"));
        void SetResponse(ReqRes Response, long? ContentLength, string? ContentType, string? ContentDisposition)
        {
            if (ContentType != null)
                Response.Response.ContentType = ContentType!;
            if (ContentLength != null)
                Response.Response.ContentLength = ContentLength.Value;
            if (ContentDisposition != null)
                Response.Response.Headers.Add("content-disposition", ContentDisposition!);
        }
        public async Task<Stream> AddReceiverAsync(ReqRes Response, CancellationToken Token = default)
        {
            _Receivers.Add(Response);
            if (IsReady())
                ReadyTaskSource.TrySetResult(true);
            await ResponseTask;
            return Response.ResponseStream;
        }
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

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
