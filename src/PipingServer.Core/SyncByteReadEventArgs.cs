using System;

namespace PipingServer.Core
{
    public readonly ref struct SyncByteReadEventArgs
    {
        public readonly ReadOnlySpan<byte> Buffer;
        public SyncByteReadEventArgs(ref ReadOnlySpan<byte> Buffer) => this.Buffer = Buffer;
    }
}
