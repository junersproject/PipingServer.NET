using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task ExecuteAsync(ActionContext context, CompletableStreamResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            var Response = context.HttpContext.Response;
            result.SetHeader(Response);
            using var l = logger.BeginLogInformationScope(nameof(ExecuteAsync));
            var Token = context.HttpContext.RequestAborted;
            try
            {
                var buffer = new byte[1024].AsMemory();
                int length;
                using (result.Stream)
                {
                    using var sl = logger.BeginLogInformationScope(nameof(ExecuteAsync) + " StreamScope");
                    try
                    {
                        while (!Token.IsCancellationRequested
                            && (length = await result.Stream.ReadAsync(buffer, Token)) > 0)
                        {
                            await Response.BodyPipe.WriteAsync(buffer.Slice(0, length), Token);
                        }
                    }
                    finally
                    {
                        Response.BodyPipe.Complete();
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
                result.FireFinally(context);
            }
        }
    }
}
