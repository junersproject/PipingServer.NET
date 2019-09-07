using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Piping.Core.Streams;

namespace Piping.Core.Pipes
{
    public interface IPipingProvider : IDisposable, IEnumerable<IPipe>
    {
        public void SetReceiver(string Path, HttpContext Receiver, ICompletableStream CompletableStream)
            => SetReceiver(Path, CompletableStream, Receiver?.RequestAborted ?? throw new ArgumentNullException(nameof(Receiver)));
        void SetReceiver(string Path, ICompletableStream CompletableStream, CancellationToken Token = default);
        public void SetSender(string Path, HttpContext Context, ICompletableStream CompletableStream)
            => SetSender(Path, Context?.Request ?? throw new ArgumentNullException(nameof(Context)), CompletableStream, Context.RequestAborted);
        void SetSender(string Path, HttpRequest Request, ICompletableStream CompletableStream, CancellationToken Token = default);
    }
    public interface IPipe
    {
        RequestKey Key { get; }
        PipeStatus Status { get; }
    }
    public interface ICompletableStream
    {
        PipeType PipeType { get; set; }
        CompletableQueueStream Stream { get; set; }
        event EventHandler? OnFinally;
        int? StatusCode { get; set; }
        IHeaderDictionary? Headers { get; set; }
        int BufferSize { get; set; }
        Task HeaderIsSetCompletedTask { get; set; }
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
