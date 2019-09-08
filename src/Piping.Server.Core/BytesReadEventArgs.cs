using System;

namespace Piping.Server.Core
{
    public class BytesReadEventArgs : EventArgs
    {
        public ReadOnlyMemory<byte> Buffer { get; }
        public BytesReadEventArgs(ReadOnlyMemory<byte> tmp) : base()
        {
            Buffer = tmp;
        }
    }
}
