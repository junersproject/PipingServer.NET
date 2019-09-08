using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Piping.Server.Core.Pipes;
using Piping.Server.Core.Streams;

namespace Piping.Server.Mvc
{
    public class CompletableStreamResult : IActionResult, ICompletableStream
    {
        public PipeType PipeType { get; set; } = PipeType.None;
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
        public IHeaderDictionary? Headers { get; set; } = null;
        public long? ContentLength => long.TryParse(Headers?["Content-Length"] ?? string.Empty, out var ContentLength) ? ContentLength : (long?)null;
        public string? ContentType => Headers?["Content-Type"];
        public string? ContentDisposition => Headers?["Content-Disposition"];
        public string? AccessControlAllowOrigin => Headers?["Access-Control-Allow-Origin"];
        public string? AccessControlExposeHeaders => Headers?["Access-Control-Expose-Headers"];
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
