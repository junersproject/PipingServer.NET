using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Piping.Server.Core.Streams;

namespace Piping.Server.Core.Pipes
{
    public interface IPipingProvider : IEnumerable<IPipe>
    {
        public Task SetReceiverAsync(RequestKey Key, HttpContext Receiver, ICompletableStream CompletableStream)
            => SetReceiverAsync(Key, CompletableStream, Receiver?.RequestAborted ?? throw new ArgumentNullException(nameof(Receiver)));
        Task SetReceiverAsync(RequestKey Key, ICompletableStream CompletableStream, CancellationToken Token = default);
        public Task SetSenderAsync(RequestKey Key, Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, HttpContext Context, ICompletableStream CompletableStream)
            => SetSenderAsync(Key, DataTask, CompletableStream, Context?.RequestAborted ?? throw new ArgumentNullException(nameof(Context)));
        Task SetSenderAsync(RequestKey Key, Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token = default);
    }
    public interface IPipingStore: IEnumerable<IPipe> {
        Task<IPipe> GetAsync(RequestKey Key, CancellationToken Token = default);
        Task<bool> TryRemoveAsync(IPipe Pipe);
    }
    public interface IPipe
    {
        RequestKey Key { get; }
        PipeStatus Status { get; }
        void AssertKey(RequestKey Key);
        ValueTask ReadyAsync(CancellationToken Token = default);
        bool IsRemovable { get; }
        int RequestedReceiversCount { get; }
        int ReceiversCount { get; }
        void SetSenderComplete();
        ValueTask SetHeadersAsync(Func<IEnumerable<ICompletableStream>,Task> SetHeaderAction);
        void AddReceiver(ICompletableStream Result);
        bool RemoveReceiver(ICompletableStream Result);
        IEnumerable<ICompletableStream> Receivers { get; }
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
