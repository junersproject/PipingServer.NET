using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Piping.Models
{
    public interface IWaiters : IDisposable
    {
        public IActionResult AddReceiver(RequestKey Key, HttpContext Receiver) => AddReceiver(Key, Receiver.RequestAborted);
        IActionResult AddReceiver(RequestKey Key, CancellationToken Token = default);
        public IActionResult AddSender(RequestKey Key, HttpContext Context, Encoding Encoding, int BufferSize) => AddSender(Key, Context.Request, Context.Response, Encoding, BufferSize, Context.RequestAborted);
        IActionResult AddSender(RequestKey Key, HttpRequest Request, HttpResponse Response, Encoding Encoding, int BufferSize, CancellationToken Token = default);
        public bool TryGet(RequestKey Key, [MaybeNullWhen(false)]out IWaiter? Waiter);
    }
    public interface IWaiter
    {
        bool IsEstablished { get; }
        bool IsSetSenderComplete { get; }
        bool ReceiversIsEmpty { get; }
        int? ReceiversCount { get; set; }
    }
}
