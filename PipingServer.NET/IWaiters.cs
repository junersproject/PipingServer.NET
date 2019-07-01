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
        int ReceiversCount { get; set; }
        bool RemoveReceiver(CompletableStreamResult Result);
        public Task<CompletableStreamResult> AddReceiverAsync(HttpContext Receiver) => AddReceiverAsync(Receiver.RequestAborted);
        Task<CompletableStreamResult> AddReceiverAsync(CancellationToken Token = default);
        public Task<CompletableStreamResult> AddSenderAsync(RequestKey Key, HttpContext Context, Encoding Encoding, int BufferSize) => AddSenderAsync(Key, Context.Request, Context.Response, Encoding, BufferSize, Context.RequestAborted);
        Task<CompletableStreamResult> AddSenderAsync(RequestKey Key, HttpRequest Request, HttpResponse Response, Encoding Encoding, int BufferSize, CancellationToken Token = default);
        bool IsReady();
    }
}
