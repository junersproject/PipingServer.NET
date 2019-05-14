using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.ServiceModel.Web;
using HttpMultipartParser;
using System.Net;

namespace Piping
{
    public class SenderResponseWaiters : IDisposable
    {
        TaskCompletionSource<bool> ReadyTaskSource = new TaskCompletionSource<bool>();
        TaskCompletionSource<bool> ResponseTasksource = new TaskCompletionSource<bool>();
        Task<bool> ReadyTask => ReadyTaskSource.Task;
        Task<bool> ResponseTask => ResponseTasksource.Task;
        ReqRes Sender = null;
        List<ReqRes> Responses = new List<ReqRes>();
        int ResponseCount = 1;
        public SenderResponseWaiters(int ResponseCount) => this.ResponseCount = ResponseCount;
        public bool IsReady() => Sender != null && Responses.Count == ResponseCount;
        public async Task<Stream> AddSenderAsync(ReqRes Sender, CancellationToken Token = default)
        {
            this.Sender = Sender;
            var IsMultiForm = (Sender.Request.Headers[HttpResponseHeader.ContentType] ?? "").IndexOf("multipart/form-data") > 0;
            var MultiFormTask = IsMultiForm ? GetPartStreamAsync(Sender, Token) : null;
            if (IsReady())
                ReadyTaskSource.TrySetResult(true);
            else
                await ReadyTask;
            var (Stream, ContentLength, ContentType, ContentDisposition) = IsMultiForm ? await MultiFormTask : GetRequestStream(Sender);
            var Buffers = new List<BufferStream>();
            foreach (var Response in Responses)
            {
                SetResponse(Response, ContentLength, ContentType, ContentDisposition);
                Buffers.Add(new BufferStream());
            }
            _ = PipingAsync(Sender.RequestStream, Buffers.ToArray(), 1024, Token);
            throw new NotImplementedException();
        }
        private static async Task PipingAsync(Stream RequestStream, IEnumerable<Stream> Buffers, int BufferSize, CancellationToken Token = default)
        {
            var buffer = new byte[BufferSize];
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            while ((bytesRead = await RequestStream.ReadAsync(buffer, 0, buffer.Length, Token).ConfigureAwait(false)) != 0)
                await Stream.WriteAsync(buffer, 0, bytesRead, Token).ConfigureAwait(false);

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
        (Stream Stream, long CountentLength, string ContentType, string ContentDisposition) GetRequestStream(ReqRes Sender)
            => (Sender.RequestStream, Sender.Request.ContentLength, Sender.Request.ContentType, Sender.Request.Headers["content-disposition"]);
        void SetResponse(ReqRes Response, long ContentLength, string ContentType, string ContentDisposition)
        {
            Response.Response.ContentType = ContentType;
            Response.Response.ContentLength = ContentLength;
            Response.Response.Headers.Add("content-disposition", ContentDisposition);
        }
        public async Task<Stream> AddResponseAsync(ReqRes Response, CancellationToken Token = default)
        {
            throw new NotImplementedException();
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
