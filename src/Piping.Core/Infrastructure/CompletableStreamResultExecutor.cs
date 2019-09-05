using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Piping.Core.Infrastructure
{
    public class CompletableStreamResultExecutor : IActionResultExecutor<CompletableStreamResult>
    {
        readonly ILogger<CompletableStreamResultExecutor> logger;
        public CompletableStreamResultExecutor(ILogger<CompletableStreamResultExecutor> logger)
        {
            this.logger = logger;
        }
        protected void SetHeader(CompletableStreamResult Result, HttpResponse Response)
        {
            if (Result.StatusCode is int _StatusCode)
                Response.StatusCode = _StatusCode;
            if (Result.AccessControlAllowOrigin is string _AccessControlAllowOrigin)
                Response.Headers["Access-Control-Allow-Origin"] = _AccessControlAllowOrigin;
            if (Result.AccessControlExposeHeaders is string _AccessControlExposeHeaders)
                Response.Headers["Access-Control-Expose-Headers"] = _AccessControlExposeHeaders;
            if (Result.ContentLength is long _ContentLength)
                Response.ContentLength = _ContentLength;
            else
                Response.ContentLength = null;
            if (Result.ContentType is string _ContentType)
                Response.ContentType = _ContentType;
            if (Result.ContentDisposition is string _ContentDisposition)
                Response.Headers["Content-Disposition"] = _ContentDisposition;
        }
        protected void SetTimeout(HttpResponse Response)
        {
            Response.StatusCode = 408;
            Response.ContentLength = 0;
            Response.Headers["Connection"] = "Close";
        }
        public async Task ExecuteAsync(ActionContext context, CompletableStreamResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            using var finallydispose = Disposable.Create(() => result.FireFinally(context));
            var Response = context.HttpContext.Response;
            try
            {
                await result.HeaderIsSetCompletedTask;
            }
            catch (OperationCanceledException e)
            {
                SetTimeout(Response);
                logger.LogError(e, "[TIMEOUT] " + e.Message);
                return;
            }
            SetHeader(result, Response);
            var Token = context.HttpContext.RequestAborted;

            try
            {
                var buffer = new byte[1024].AsMemory();
                int length;
                using (result.Stream)
                {
                    try
                    {
                        while (!Token.IsCancellationRequested
                            && (length = await result.Stream.ReadAsync(buffer, Token)) > 0)
                        {
                            await Response.BodyWriter.WriteAsync(buffer.Slice(0, length), Token);
                        }
                    }
                    finally
                    {
                        Response.BodyWriter.Complete();
                    }

                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogError(e, "[CANCELED] " + e.Message);
            }
            catch (Exception e)
            {
                logger.LogError(e, "[ERROR] " + e.Message);
                throw;
            }
        }
    }
}
