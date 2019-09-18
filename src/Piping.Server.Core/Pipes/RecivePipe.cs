using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Piping.Server.Core.Internal;
using Piping.Server.Core.Streams;
using static Piping.Server.Core.Properties.Resources;

namespace Piping.Server.Core.Pipes
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

        public int RequestedReceiversCount => Current.RequestedReceiversCount;

        public int ReceiversCount => Current.ReceiversCount;

        public event EventHandler? OnFinally
        {
            add => Current.OnFinally += value;
            remove => Current.OnFinally -= value;
        }
        public event PipeStatusChangeEventHandler? OnStatusChanged
        {
            add => Current.OnStatusChanged += value;
            remove => Current.OnStatusChanged -= value;
        }
        public async ValueTask ConnectionAsync(ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            using var finallyremove = Disposable.Create(() => Current.TryRemove());
            SetReceiverCompletableStream(CompletableStream);
            AddReceiver(CompletableStream);
            await Task.WhenAny(ReadyAsync().AsTask(), Token.AsTask());
        }
        const string AccessControlAllowOriginKey = "Access-Control-Allow-Origin";
        const string AccessControlAllowOriginValue = " * ";
        const string AccessControlExposeHeadersKey = "Access-Control-Expose-Headers";
        const string AccessControlExposeHeaderValue = "Content-Length, Content-Type";
        void SetReceiverCompletableStream(ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Receiver;
            if (CompletableStream.Stream == PipelineStream.Empty)
                CompletableStream.Stream = new PipelineStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers[AccessControlAllowOriginKey] = AccessControlAllowOriginValue;
            CompletableStream.Headers[AccessControlExposeHeadersKey] = AccessControlExposeHeaderValue;
            CompletableStream.OnFinally += (o, arg) =>
            {
                var Removed = RemoveReceiver(CompletableStream);
                if (Removed)
                    Logger.LogDebug(string.Format(StreamRemoveSuccess, CompletableStream));
                else
                    Logger.LogDebug(string.Format(StreamRemoveFaild, CompletableStream));
                Current.TryRemove();
            };
        }
        public void AddReceiver(ICompletableStream Result) => Current.AddReceiver(Result);

        public ValueTask ReadyAsync(CancellationToken Token = default) => Current.ReadyAsync(Token);

        public bool RemoveReceiver(ICompletableStream Result) => Current.RemoveReceiver(Result);
        public override string ToString() => Current?.ToString() ?? string.Empty;
    }
}
