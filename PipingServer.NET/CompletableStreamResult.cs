using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Piping.Streams;

namespace Piping
{
    public class CompletableStreamResult : IActionResult
    {
        readonly ILogger<CompletableStreamResult> logger;
        public CompletableQueueStream Stream { get; set; } = CompletableQueueStream.Empty;
        public event EventHandler OnFinally; 
        public int? StatusCode { get; set; }
        public long? ContentLength { get; set; } = null;
        public string? ContentType { get; set; } = null;
        public string? ContentDisposition { get; set; } = null;
        public string? AccessControlAllowOrigin { get; set; } = null;
        public string? AccessControlExposeHeaders { get; set; } = null;
        public int BufferSize { get; set; } = 1024;
        public CompletableStreamResult(ILogger<CompletableStreamResult> logger)
            => this.logger = logger;
        public CompletableStreamResult(ILogger<CompletableStreamResult> logger, CompletableQueueStream? Stream = null, long? ContentLength = null, string? ContentType = null, string? ContentDisposition = null)
            => (this.logger, this.Stream, this.ContentLength, this.ContentType, this.ContentDisposition) = (logger, Stream ?? CompletableQueueStream.Empty, ContentLength, ContentType, ContentDisposition);
        protected void SetHeader(HttpResponse Response)
        {
            using var l = logger.BeginLogInformationScope(nameof(SetHeader));
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
        }
        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var l = logger.BeginLogInformationScope(nameof(ExecuteResultAsync));
            var Token = context.HttpContext.RequestAborted;
            var Response = context.HttpContext.Response;
            try
            {
                using var lt = logger.BeginLogInformationScope(nameof(ExecuteResultAsync) + " try Scope");
                SetHeader(Response);
                var buffer = new Memory<byte>();
                int length;
                using (Stream)
                {
                    using var sl = logger.BeginLogInformationScope(nameof(ExecuteResultAsync) + " StreamScope");
                    while (!Token.IsCancellationRequested
                        && (length = await Stream.ReadAsync(buffer, Token)) > 0)
                    {
                        await Response.Body.WriteAsync(buffer.Slice(0, length), Token);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogInformation(e.Message, e);
            }
            catch (Exception e)
            {
                logger.LogWarning(e.Message, e);
                throw;
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
