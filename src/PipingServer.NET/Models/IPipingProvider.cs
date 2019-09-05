using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Piping.Models
{
    public interface IPipingProvider : IDisposable
    {
        public IActionResult AddReceiver(string Path, HttpContext Receiver)
            => AddReceiver(Path, Receiver?.RequestAborted ?? throw new ArgumentNullException(nameof(Receiver)));
        IActionResult AddReceiver(string Path, CancellationToken Token = default);
        public IActionResult AddSender(string Path, HttpContext Context)
            => AddSender(Path, Context?.Request ?? throw new ArgumentNullException(nameof(Context)), Context.RequestAborted);
        IActionResult AddSender(string Path, HttpRequest Request, CancellationToken Token = default);
    }
    public interface IPipe
    {
        RequestKey Key { get; }
        PipeStatus Status { get; }
    }
    public enum PipeStatus
    {
        Wait,
        Ready,
        ResponseStart,
        Canceled,
    }
}
