using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping
{
    public interface IWaiters : IDisposable
    {
        bool IsEstablished { get; }
        bool IsSetSenderComplete { get; }
        bool ReceiversIsEmpty { get; }
        void DecrementReceivers();
        public Task<CompletableStreamResult> AddReceiverAsync(HttpContext Receiver) => AddReceiverAsync(Receiver.Response, Receiver.RequestAborted);
        Task<CompletableStreamResult> AddReceiverAsync(HttpResponse Receiver, CancellationToken Token = default);
        public Task<CompletableStreamResult> AddSenderAsync(RequestKey Key, HttpContext Context, Encoding Encoding, int BufferSize) => AddSenderAsync(Key, Context.Request, Context.Response, Encoding, BufferSize, Context.RequestAborted);
        Task<CompletableStreamResult> AddSenderAsync(RequestKey Key, HttpRequest Request, HttpResponse Response, Encoding Encoding, int BufferSize, CancellationToken Token = default);
        bool IsReady();
    }
}
