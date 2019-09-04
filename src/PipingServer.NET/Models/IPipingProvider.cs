using System;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Piping.Models
{
    public interface IPipingProvider : IDisposable
    {
        public IActionResult AddReceiver(string RelativeUri, HttpContext Receiver) => AddReceiver(RelativeUri, Receiver.RequestAborted);
        IActionResult AddReceiver(string RelativeUri, CancellationToken Token = default);
        public IActionResult AddSender(string RelativeUri, HttpContext Context) => AddSender(RelativeUri, Context.Request, Context.RequestAborted);
        IActionResult AddSender(string RelativeUri, HttpRequest Request, CancellationToken Token = default);
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
