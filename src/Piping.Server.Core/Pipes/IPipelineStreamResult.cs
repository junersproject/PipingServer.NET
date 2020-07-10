using System;
using Microsoft.AspNetCore.Http;
using Piping.Server.Core.Streams;

namespace Piping.Server.Core.Pipes
{
    public interface IPipelineStreamResult
    {
        PipeType PipeType { get; set; }
        PipelineStream Stream { get; set; }
        event EventHandler? OnFinally;
        int? StatusCode { get; set; }
        IHeaderDictionary? Headers { get; set; }
    }
}
