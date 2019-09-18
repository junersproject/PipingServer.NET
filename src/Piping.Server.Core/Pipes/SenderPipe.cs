using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Piping.Server.Core.Internal;
using Piping.Server.Core.Streams;
using static Piping.Server.Core.Properties.Resources;

namespace Piping.Server.Core.Pipes
{
    internal class SenderPipe : ISenderPipe
    {
        readonly Pipe Current;
        readonly PipingOptions Options;
        readonly ILogger<SenderPipe> Logger;
        internal SenderPipe(Pipe Current, PipingOptions Options, ILogger<SenderPipe> Logger)
            => (this.Current, this.Options, this.Logger) = (Current, Options, Logger);
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

        public ValueTask ReadyAsync(CancellationToken Token = default) => Current.ReadyAsync(Token);

        public async ValueTask SetHeadersAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, CancellationToken Token = default)
        {
            await Current.SetHeadersAsync(DataTask, Token);
        }

        public async ValueTask ConnectionAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            using var finallyremove = Disposable.Create(() => Current.TryRemove());
            using var l = Logger?.LogDebugScope(nameof(ConnectionAsync));
            SetSenderCompletableStream(CompletableStream);
            var SetHeaderTask = SetHeadersAsync(DataTask, Token);
            await SendMessageAsync(CompletableStream.Stream, string.Format(WaitingForRequestedReceiversCountReceivers, Current.RequestedReceiversCount), Token);
            await SendMessageAsync(CompletableStream.Stream, string.Format(ReceiversCountReceiversHaveBeenConnected, Current.ReceiversCount), Token);
            await SetHeaderTask;
            _ = SetSenderAsync(DataTask, CompletableStream, Token);
        }
        const string ContentTypeKey = "Content-Type";
        const string SenderResponseMessageMimeType = "text/plain;charset={0}";
        void SetSenderCompletableStream(ICompletableStream CompletableStream)
        {
            CompletableStream.PipeType = PipeType.Sender;
            if (CompletableStream.Stream == PipelineStream.Empty)
                CompletableStream.Stream = new PipelineStream();
            CompletableStream.Headers ??= new HeaderDictionary();
            CompletableStream.Headers[ContentTypeKey] = string.Format(SenderResponseMessageMimeType, Options.Encoding.WebName);
            CompletableStream.OnFinally += (o, arg) => Current.TryRemove();
        }
        async Task SetSenderAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token)
        {
            using var l = Logger?.LogDebugScope(nameof(SetSenderAsync));
            try
            {
                var (Headers, Stream) = await DataTask;
                var PipingTask = PipingAsync(Stream, CompletableStream.Stream, Current.Receivers.Select(v => v.Stream), Options.BufferSize, Token);
                await SendMessageAsync(CompletableStream.Stream, string.Format(StartSendingWithReceiversCountReceivers, Current.ReceiversCount));
                await PipingTask;
            }
            catch (Exception e)
            {
                Logger.LogError(e, nameof(SetSenderAsync));
            }
        }


        async Task SendMessageAsync(Stream Stream, string Message, CancellationToken Token = default)
        {
            Logger.LogDebug(Message);
            await Stream.WriteAsync(Options.Encoding.GetBytes(string.Format(InfoPrefix, Message) + Environment.NewLine).AsMemory(), Token);
        }

        async Task PipingAsync(Stream RequestStream, PipelineStream InfomationStream, IEnumerable<PipelineStream> Buffers, int BufferSize, CancellationToken Token = default)
        {
            using var l = Logger.LogDebugScope(nameof(PipingAsync));
            var buffer = new byte[BufferSize].AsMemory();
            using var Stream = new PipingStream(Buffers);
            int bytesRead;
            var byteCounter = 0L;
            using var finallyact = Disposable.Create(() =>
            {
                foreach (var b in Buffers)
                    b.Complete();
                InfomationStream.Complete();
            });
            while ((bytesRead = await RequestStream.ReadAsync(buffer, Token).ConfigureAwait(false)) != 0)
            {
                await Stream.WriteAsync(buffer.Slice(0, bytesRead), Token).ConfigureAwait(false);
                byteCounter += bytesRead;
            }
            await SendMessageAsync(InfomationStream, string.Format(SendingSuccessfulBytes, byteCounter));
        }
    }
}
