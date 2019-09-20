using System;

namespace Piping.Server.Core.Pipes
{
    public interface IPipe
    {
        RequestKey Key { get; }
        PipeStatus Status { get; }
        bool IsRemovable { get; }
        int RequestedReceiversCount { get; }
        int ReceiversCount { get; }
    }
}
