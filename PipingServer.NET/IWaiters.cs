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

        Task<Stream> AddReceiverAsync(HttpContext Response, CancellationToken Token = default);
        Stream AddSender(RequestKey Key, HttpContext Sender, Encoding Encoding, int BufferSize, CancellationToken Token = default);
        bool IsReady();
        bool UnRegisterReceiver(HttpContext Receiver);
    }
}
