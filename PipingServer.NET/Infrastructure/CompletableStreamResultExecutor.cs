using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Piping.Infrastructure
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
            using var l = logger.BeginLogInformationScope(nameof(SetHeader) + " : " + Result.Identity);
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
        public async Task ExecuteAsync(ActionContext context, CompletableStreamResult result)
        {
            using var l = logger.BeginLogInformationScope(nameof(ExecuteAsync) + " : " + result.Identity);
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            var Response = context.HttpContext.Response;
            await result.HeaderIsSetCompletedTask;
            SetHeader(result, Response);
            var Token = context.HttpContext.RequestAborted;
            try
            {
                var buffer = new byte[1024].AsMemory();
                int length;
                using (result.Stream)
                {
                    using var sl = logger.BeginLogInformationScope(nameof(ExecuteAsync) + " : " + result.Identity + " StreamScope");
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
                logger.LogInformation(e.Message, e);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message, e);
                throw;
            }
            finally
            {
                result.FireFinally(context);
            }
        }
    }
}
