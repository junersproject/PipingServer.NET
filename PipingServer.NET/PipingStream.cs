using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Piping
{
    public class PipingStream : Stream
    {
        readonly IDisposable[] Disposables;
        public PipingStream(params Stream[] outputStreams) : this((IEnumerable<Stream>)outputStreams) { }
        public PipingStream(IEnumerable<Stream> outputStreams) : base()
        {
            Disposables = outputStreams.Select(stream =>
            {
                BytesRead += action;
                return Disposable.Create(() => BytesRead -= action);
                void action(object self, BytesReadEventArgs args)
                {
                    stream.Write(args.Buffer, 0, args.Buffer.Length);
                }
            }).ToArray();
        }
        public event EventHandler<BytesReadEventArgs> BytesRead;
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin)
            => throw new InvalidOperationException("Cannot seek in " + nameof(PipingStream));
        public override void SetLength(long value) => throw new InvalidOperationException();
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException();
        private void PipeToOutputStream(byte[] buffer, int offset, int numberOfBytesRead)
        {
            var tmp = new byte[numberOfBytesRead];
            Array.Copy(buffer, offset, tmp, 0, numberOfBytesRead);
            BytesRead?.Invoke(this, new BytesReadEventArgs(tmp));
        }
        public override void Write(byte[] buffer, int offset, int count) => PipeToOutputStream(buffer, offset, count);
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new InvalidOperationException();
        public override long Position
        {
            get { throw new InvalidOperationException(); }
            set { throw new InvalidOperationException("Cannot set position in CachingStream."); }
        }
        public override void Close()
        {
            base.Close();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach (var disposable in Disposables)
                disposable.Dispose();
        }
    }
}
