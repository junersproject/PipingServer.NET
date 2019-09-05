using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Piping.Core.Streams;

namespace Piping.Core
{
    public class CompletableStreamResult : IActionResult
    {
        public string Identity = string.Empty;
        public CompletableQueueStream Stream { get; set; } = CompletableQueueStream.Empty;
        public event EventHandler? OnFinally;
        public void FireFinally(ActionContext? context = null)
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
        public int? StatusCode { get; set; }
        public long? ContentLength { get; set; } = null;
        public string? ContentType { get; set; } = null;
        public string? ContentDisposition { get; set; } = null;
        public string? AccessControlAllowOrigin { get; set; } = null;
        public string? AccessControlExposeHeaders { get; set; } = null;
        public int BufferSize { get; set; } = 1024;
        public Task HeaderIsSetCompletedTask { get; set; } = Task.CompletedTask;
        public CompletableStreamResult() { }
        public Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<CompletableStreamResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
