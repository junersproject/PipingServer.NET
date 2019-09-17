using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Piping.Server.Core.Internal;
using Piping.Server.Core.Streams;

namespace Piping.Server.Core.Pipes
{
    internal class RecivePipe : IRecivePipe
    {
        readonly Pipe Current;
        readonly ILogger<IRecivePipe> Logger;
        internal RecivePipe(Pipe Current, ILogger<IRecivePipe> Logger)
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
        void SetReceiverCompletableStream(ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Receiver;
            if (CompletableStream.Stream == CompletableQueueStream.Empty)
                CompletableStream.Stream = new CompletableQueueStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers["Access-Control-Allow-Origin"] = " * ";
            CompletableStream.Headers["Access-Control-Expose-Headers"] = "Content-Length, Content-Type";
            CompletableStream.OnFinally += (o, arg) =>
            {
                var Removed = RemoveReceiver(CompletableStream);
                Logger.LogDebug("STREAM REMOVE " + (Removed ? "SUCCESS" : "FAILED"));
                Current.TryRemove();
            };
        }
        public void AddReceiver(ICompletableStream Result) => Current.AddReceiver(Result);

        public ValueTask ReadyAsync(CancellationToken Token = default) => Current.ReadyAsync(Token);

        public bool RemoveReceiver(ICompletableStream Result) => Current.RemoveReceiver(Result);
        public override string ToString() => Current?.ToString() ?? string.Empty;
    }
}
