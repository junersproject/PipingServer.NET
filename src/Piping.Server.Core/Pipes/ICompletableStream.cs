using System;
using Microsoft.AspNetCore.Http;
using Piping.Server.Core.Streams;

namespace Piping.Server.Core.Pipes
{
    public interface ICompletableStream
    {
        PipeType PipeType { get; set; }
        CompletableQueueStream Stream { get; set; }
        event EventHandler? OnFinally;
        int? StatusCode { get; set; }
        IHeaderDictionary? Headers { get; set; }
    }
}
