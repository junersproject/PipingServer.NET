using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Piping.Server.Mvc.Internal;

namespace Piping.Server.Mvc.Infrastructure
{
    public class CompletableStreamResultExecutor : IActionResultExecutor<PipelineStreamResult>
    {
        readonly ILogger<CompletableStreamResultExecutor> logger;
        /// <summary>
        /// piping allow headers
        /// </summary>
        readonly static ISet<string> AllowHeaders = new HashSet<string>
        {
            "content-type",
            "content-length",
            "content-disposition",
            "access-control-allow-origin",
            "access-control-expose-headers",
        };
        public CompletableStreamResultExecutor(ILogger<CompletableStreamResultExecutor> logger)
        {
            this.logger = logger;
        }
        /// <summary>
        /// ヘッダーを設定する
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="Response"></param>
        protected void SetHeader(PipelineStreamResult Result, HttpResponse Response)
        {
            if (Result.StatusCode is int _StatusCode)
                Response.StatusCode = _StatusCode;
            if (Result.Headers is IHeaderDictionary Headers)
                foreach (var kv in Headers.ToList())
                    if (AllowHeaders.Contains(kv.Key.ToLower()))
                        Response.Headers[kv.Key] = kv.Value;
        }
        protected void SetTimeout(HttpResponse Response)
        {
            Response.StatusCode = 408;
            Response.ContentLength = 0;
            Response.Headers["Connection"] = "Close";
        }
        public async Task ExecuteAsync(ActionContext context, PipelineStreamResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            using var finallydispose = Disposable.Create(() => result.FireFinally(context));
            var Response = context.HttpContext.Response;
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
                            await Response.Body.WriteAsync(buffer.Slice(0, length), Token);
                        }
                    }
                    finally
                    {
                        await Response.Body.FlushAsync(Token);
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
