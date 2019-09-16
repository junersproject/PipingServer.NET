using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Piping.Server.Core.Streams;

namespace Piping.Server.Core.Pipes
{
    public interface IPipingProvider
    {
        public Task SetReceiverAsync(RequestKey Key, HttpContext Receiver, ICompletableStream CompletableStream)
            => SetReceiverAsync(Key, CompletableStream, Receiver?.RequestAborted ?? throw new ArgumentNullException(nameof(Receiver)));
        Task SetReceiverAsync(RequestKey Key, ICompletableStream CompletableStream, CancellationToken Token = default);
        public Task SetSenderAsync(RequestKey Key, Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, HttpContext Context, ICompletableStream CompletableStream)
            => SetSenderAsync(Key, DataTask, CompletableStream, Context?.RequestAborted ?? throw new ArgumentNullException(nameof(Context)));
        Task SetSenderAsync(RequestKey Key, Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token = default);
    }
    public interface IPipingStore: IEnumerable<IPipe> {
        Task<ISenderPipe> GetSenderAsync(RequestKey Key, CancellationToken Token = default);
        Task<IRecivePipe> GetReceiveAsync(RequestKey Key, CancellationToken Token = default);
        Task<IPipe> GetAsync(RequestKey Key, CancellationToken Token = default);
        Task<bool> TryRemoveAsync(IPipe Pipe);
    }
    public interface IPipe
    {
        RequestKey Key { get; }
        PipeStatus Status { get; }
        ValueTask ReadyAsync(CancellationToken Token = default);
        bool IsRemovable { get; }
        int RequestedReceiversCount { get; }
        int ReceiversCount { get; }
        event EventHandler? OnWaitTimeout;
        event PipeStatusChangeEventHandler? OnStatusChanged;
    }
    public class PipeStatusChangedArgs
    {
        public PipeStatusChangedArgs() : this(PipeStatus.Wait) { }
        public PipeStatusChangedArgs(PipeStatus Status) => this.Status = Status;
        public PipeStatus Status { get; } = PipeStatus.Wait;
    }
    public delegate void PipeStatusChangeEventHandler(object? sender, PipeStatusChangedArgs args);
    public interface IReadOnlyPipe : IPipe
    {
        Task<IHeaderDictionary> GetHeadersAsync(CancellationToken Token = default);
    }
    public interface ISenderPipe : IPipe
    {
        ValueTask SetHeadersAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, CancellationToken Token = default);
        ValueTask ConnectionAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token = default);
    }
    public interface IRecivePipe : IPipe
    {
        void AddReceiver(ICompletableStream Result);
        bool RemoveReceiver(ICompletableStream Result);
        ValueTask ConnectionAsync(ICompletableStream CompletableStream, CancellationToken Token = default);
    }
    public interface ICompletableStream
    {
        PipeType PipeType { get; set; }
        CompletableQueueStream Stream { get; set; }
        event EventHandler? OnFinally;
        int? StatusCode { get; set; }
        IHeaderDictionary? Headers { get; set; }
    }
    public enum PipeStatus : byte
    {
        Wait = 0,
        Ready = 1,
        ResponseStart = 2,
        Canceled = 3,
    }
    public enum PipeType : byte
    {
        None = 0,
        Sender = 1,
        Receiver = 2,
    }
}
