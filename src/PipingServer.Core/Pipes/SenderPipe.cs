﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PipingServer.Core.Internal;
using PipingServer.Core.Options;
using PipingServer.Core.Streams;
using static PipingServer.Core.Properties.Resources;

namespace PipingServer.Core.Pipes
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

        public int ReceiversCount => Current.ReceiversCount;
        public event PipeStatusChangeEventHandler? OnStatusChanged
        {
            add => Current.OnStatusChanged += value;
            remove => Current.OnStatusChanged -= value;
        }

        public ValueTask ReadyAsync(CancellationToken Token = default) => Current.ReadyAsync(Token);

        public async ValueTask SetHeadersAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, CancellationToken Token = default)
        {
            await Current.SetInputDataAsync(DataTask, Token);
        }

        public async ValueTask ConnectionAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, IPipelineStreamResult CompletableStream, CancellationToken Token = default)
        {
            Token.ThrowIfCancellationRequested();
            using var finallyremove = Disposable.Create(() => _ = Current.TryRemoveAsync());
            using var l = Logger?.LogDebugScope(nameof(ConnectionAsync));
            SetSenderCompletableStream(CompletableStream);
            var SetHeaderTask = SetHeadersAsync(DataTask, Token);
            await SendMessageAsync(CompletableStream.Stream, string.Format(WaitingForRequestedReceiversCountReceivers, Current.Key.Receivers), Token);
            await SendMessageAsync(CompletableStream.Stream, string.Format(ReceiversCountReceiversHaveBeenConnected, Current.ReceiversCount), Token);
            await SetHeaderTask;
            _ = SetSenderAsync(DataTask, CompletableStream, Token);
        }
        const string ContentTypeKey = "Content-Type";
        void SetSenderCompletableStream(IPipelineStreamResult Result)
        {
            Result.StatusCode = 200;
            Result.PipeType = PipeType.Sender;
            if (Result.Stream == PipelineStream.Empty)
                Result.Stream = new PipelineStream();
            Result.Headers[ContentTypeKey] = string.Format(Options.SenderResponseMessageContentType ?? SenderResponseMessageMimeType, Options.Encoding.WebName);
            Result.OnFinally += (o, arg) => _ = Current.TryRemoveAsync();
        }
        async Task SetSenderAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, IPipelineStreamResult CompletableStream, CancellationToken Token)
        {
            using var l = Logger?.LogDebugScope(nameof(SetSenderAsync));
            try
            {
                using var s = Disposable.Create(() => CompletableStream.Stream.Complete());
                var (Headers, Stream) = await DataTask;
                LoggingHeader(Headers);
                var PipingTask = Current.PipingAsync(Token);
                await SendMessageAsync(CompletableStream.Stream, string.Format(StartSendingWithReceiversCountReceivers, Current.ReceiversCount));
                var byteCounter = await PipingTask;
                await SendMessageAsync(CompletableStream.Stream, string.Format(SendingSuccessfulBytes, byteCounter));
            }
            catch (Exception e)
            {
                Logger.LogError(e, nameof(SetSenderAsync));
            }
        }
        void LoggingHeader(IHeaderDictionary Headers)
        {
            if (Logger?.IsEnabled(LogLevel.Information) ?? false)
            {
                foreach (var header in Headers)
                    Logger.LogInformation($"SENDER HEADER: {header.Key}: {header.Value}");
            }
        }

        async Task SendMessageAsync(Stream Stream, string Message, CancellationToken Token = default)
        {
            Logger.LogDebug(Message);
            await Stream.WriteAsync(Options.Encoding.GetBytes(string.Format(InfoPrefix, Message) + Environment.NewLine).AsMemory(), Token);
        }
    }
}
