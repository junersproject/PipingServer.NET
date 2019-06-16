using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Piping.Streams;

namespace Piping
{
    public class CompletableStreamResult : IActionResult
    {
        public CompletableQueueStream Stream { get; set; } = CompletableQueueStream.Empty;
        public event EventHandler OnFinally; 
        public int? StatusCode { get; set; }
        public long? ContentLength { get; set; } = null;
        public string? ContentType { get; set; } = null;
        public string? ContentDisposition { get; set; } = null;
        public string? AccessControlAllowOrigin { get; set; } = null;
        public string? AccessControlExposeHeaders { get; set; } = null;
        public int BufferSize { get; set; } = 1024;
        public CompletableStreamResult(){ }
        public CompletableStreamResult(CompletableQueueStream? Stream = null, long? ContentLength = null, string? ContentType = null, string? ContentDisposition = null)
            => (this.Stream, this.ContentLength, this.ContentType, this.ContentDisposition) = (Stream ?? CompletableQueueStream.Empty, ContentLength, ContentType, ContentDisposition);

        public async Task ExecuteResultAsync(ActionContext context)
        {
            var Token = context.HttpContext.RequestAborted;
            var Response = context.HttpContext.Response;
            if (StatusCode is int _StatusCode)
                Response.StatusCode = _StatusCode;
            if (AccessControlAllowOrigin is string _AccessControlAllowOrigin)
                Response.Headers["Access-Control-Allow-Origin"] = _AccessControlAllowOrigin;
            if (AccessControlExposeHeaders is string _AccessControlExposeHeaders)
                Response.Headers["Access-Control-Expose-Headers"] = _AccessControlExposeHeaders;
            if (ContentLength is long _ContentLength)
                Response.ContentLength = _ContentLength;
            else
                Response.ContentLength = null;
            if (ContentType is string _ContentType)
                Response.ContentType = _ContentType;
            if (ContentDisposition is string _ContentDisposition)
                Response.Headers["Content-Disposition"] = _ContentDisposition;
            try
            {
                var buffer = new Memory<byte>();
                int length;
                using (Stream)
                    while (!Token.IsCancellationRequested
                        && (length = await Stream.ReadAsync(buffer, Token)) > 0)
                    {
                        await Response.Body.WriteAsync(buffer.Slice(0, length), Token);
                        await Response.Body.FlushAsync(Token);
                    }
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                try
                {
                    OnFinally?.Invoke(context, new EventArgs());
                }
                catch (Exception)
                {
                    
                }
                foreach (var d in (OnFinally?.GetInvocationList() ?? Enumerable.Empty<Delegate>()).Cast<EventHandler>())
                    OnFinally -= d;
            }
        }
    }
}
