using System;
namespace PipingServer.Core.Pipes
{
    [Flags]
    public enum PipeType : byte
    {
        Unuse = 0b_0000,
        None = 0b_0001,
        Sender = 0b_0010,
        Receiver = 0b_0100,
        All = Sender | Receiver,
    }
}
