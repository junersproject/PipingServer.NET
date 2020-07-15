using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PipingServer.Core.Internal;
using PipingServer.Core.Streams;
using static PipingServer.Core.Properties.Resources;

namespace PipingServer.Core.Pipes
{
    internal class RecivePipe : IRecivePipe
    {
        readonly Pipe Current;
        readonly ILogger<RecivePipe> Logger;
        internal RecivePipe(Pipe Current, ILogger<RecivePipe> Logger)
            => (this.Current, this.Logger) = (Current, Logger);
        public RequestKey Key => Current.Key;

        public PipeStatus Status => Current.Status;

        public bool IsRemovable => Current.IsRemovable;

        public int ReceiversCount => Current.ReceiversCount;
        public async ValueTask ConnectionAsync(IPipelineStreamResult CompletableStream, CancellationToken Token = default)
        {
            using var finallyremove = Disposable.Create(() => Current.TryRemove());
            SetReceiverCompletableStream(CompletableStream);
            Current.AddReceiver(CompletableStream);
            await Current.ResponseReady(Token);
        }
        const string AccessControlAllowOriginKey = "Access-Control-Allow-Origin";
        const string AccessControlAllowOriginValue = " * ";
        const string AccessControlExposeHeadersKey = "Access-Control-Expose-Headers";
        const string AccessControlExposeHeaderValue = "Content-Length, Content-Type";
        const string XContentTypeOptionsKey = "X-Content-Type-Options";
        const string XContentTypeOptionsValue = "nosniff";
        void SetReceiverCompletableStream(IPipelineStreamResult Result)
        {
            Result.StatusCode = 200;
            Result.PipeType = PipeType.Receiver;
            if (Result.Stream == PipelineStream.Empty)
                Result.Stream = new PipelineStream();
            Result.Headers[AccessControlAllowOriginKey] = AccessControlAllowOriginValue;
            Result.Headers[AccessControlExposeHeadersKey] = AccessControlExposeHeaderValue;
            Result.Headers[XContentTypeOptionsKey] = XContentTypeOptionsValue;
            Result.OnFinally += (o, arg) =>
            {
                var Removed = Current.RemoveReceiver(Result);
                if (Removed)
                    Logger.LogDebug(string.Format(StreamRemoveSuccess, Result));
                else
                    Logger.LogDebug(string.Format(StreamRemoveFaild, Result));
                Current.TryRemove();
            };
        }
        public override string ToString() => Current.ToString()!;
    }
}
