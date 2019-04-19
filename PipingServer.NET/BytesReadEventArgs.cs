using System;

namespace Piping
{
    public class BytesReadEventArgs : EventArgs
    {
        public byte[] Buffer { get; }
        public BytesReadEventArgs(byte[] tmp) : base()
        {
            Buffer = tmp;
        }
    }
}
