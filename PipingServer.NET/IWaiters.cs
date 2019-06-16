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
        public Task<CompletableStreamResult> AddReceiverAsync(HttpContext Receiver) => AddReceiverAsync(Receiver.Response, Receiver.RequestAborted);
        Task<CompletableStreamResult> AddReceiverAsync(HttpResponse Receiver, CancellationToken Token = default);
        public CompletableStreamResult AddSender(RequestKey Key, HttpContext Context, Encoding Encoding, int BufferSize) => AddSender(Key, Context.Request, Context.Response, Encoding, BufferSize, Context.RequestAborted);
        CompletableStreamResult AddSender(RequestKey Key, HttpRequest Request, HttpResponse Response, Encoding Encoding, int BufferSize, CancellationToken Token = default);
        bool IsReady();
        bool UnRegisterReceiver(CompletableStreamResult Receiver);
    }
}
