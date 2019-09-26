using System;
namespace Piping.Server.Core.Pipes
{
    [Flags]
    public enum PipeType : byte
    {
        None = 0,
        Sender = 1,
        Receiver = 2,
        All = Sender | Receiver,
    }
}
