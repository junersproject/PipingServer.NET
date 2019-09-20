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
    public class PipelineStreamResult : IActionResult, IPipelineStreamResult
    {
        public PipeType PipeType { get; set; } = PipeType.None;
        public PipelineStream Stream { get; set; } = PipelineStream.Empty;
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
        public PipelineStreamResult() { }
        public Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<PipelineStreamResult>>();
            return executor.ExecuteAsync(context, this);
        }
    }
}
