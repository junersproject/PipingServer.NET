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

        public event EventHandler? OnWaitTimeout
        {
            add => Current.OnWaitTimeout += value;
            remove => Current.OnWaitTimeout -= value;
        }
        public event PipeStatusChangeEventHandler? OnStatusChanged
        {
            add => Current.OnStatusChanged += value;
            remove => Current.OnStatusChanged -= value;
        }
        public ValueTask ConnectionAsync(ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            using var l = Logger?.LogDebugScope(nameof(ConnectionAsync));
            SetSenderCompletableStream(Waiter, CompletableStream);
            _ = Waiter.SetHeadersAsync(DataTask, Token);
            await SendMessageAsync(CompletableStream.Stream, $"Waiting for {Waiter.RequestedReceiversCount} receiver(s)...", Token);
            await SendMessageAsync(CompletableStream.Stream, $"{Waiter.ReceiversCount} receiver(s) has/have been connected.", Token);
            await Waiter.ReadyAsync(Token);
            _ = _SetSenderAsync(DataTask, CompletableStream, Waiter, Token);

        }

        private void SetSenderCompletableStream(IPipe Waiter, ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Sender;
            if (CompletableStream.Stream == CompletableQueueStream.Empty)
                CompletableStream.Stream = new CompletableQueueStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers["Content-Type"] = $"text/plain;charset={Options.Encoding.WebName}";
            CompletableStream.OnFinally += (o, arg) => Store.TryRemoveAsync(Waiter);
        }
        public void AddReceiver(ICompletableStream Result) => Current.AddReceiver(Result);

        public ValueTask ReadyAsync(CancellationToken Token = default) => Current.ReadyAsync(Token);

        public bool RemoveReceiver(ICompletableStream Result) => Current.RemoveReceiver(Result);
        public override string ToString() => Current?.ToString() ?? string.Empty;
    }
}
