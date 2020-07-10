using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace PipingServer.Mvc.Infrastructure
{
    public class CompletableStreamResultExecutor : IActionResultExecutor<PipelineStreamResult>
    {
        readonly ILogger<CompletableStreamResultExecutor> logger;
        /// <summary>
        /// piping allow headers
        /// </summary>
        static readonly ISet<string> AllowHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
            {
                Response.StatusCode = _StatusCode;
                logger.LogInformation($"SET STATUS CODE: {_StatusCode}");
            }
            if (Result.Headers is IHeaderDictionary Headers)
                foreach (var kv in Headers.ToList())
                    if (AllowHeaders.Contains(kv.Key.ToLower()))
                    {
                        Response.Headers[kv.Key] = kv.Value;
                        logger.LogInformation($"{Result.PipeType} SET HEADER: {kv.Key}: {kv.Value}");
                    }
        }
        public async Task ExecuteAsync(ActionContext context, PipelineStreamResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            using (result)
            {
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
}
