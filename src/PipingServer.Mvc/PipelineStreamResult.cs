using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using PipingServer.Core.Pipes;
using PipingServer.Core.Streams;

namespace PipingServer.Mvc
{
    public class PipelineStreamResult : IActionResult, IPipelineStreamResult, IDisposable
    {
        public PipeType PipeType { get; set; } = PipeType.None;
        public PipelineStream Stream { get; set; } = PipelineStream.Empty;
        public event EventHandler? OnFinally;
        public int? StatusCode { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public PipelineStreamResult() { }
        public Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<PipelineStreamResult>>();
            return executor.ExecuteAsync(context, this);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    OnFinally?.Invoke(this, new EventArgs());
                    foreach (var d in (OnFinally?.GetInvocationList() ?? Enumerable.Empty<Delegate>()).Cast<EventHandler>())
                        OnFinally -= d;
                }
                disposedValue = true;
            }
        }
        ~PipelineStreamResult() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
