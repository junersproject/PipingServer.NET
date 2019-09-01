using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Piping.Streams
{
    public class PipingStream : Stream
    {
        readonly IDisposable[] Disposables;
        public PipingStream(params Stream[] outputStreams) : this((IEnumerable<Stream>)outputStreams) { }
        public PipingStream(IEnumerable<Stream> outputStreams) : base()
        {
            Disposables = outputStreams.Select(stream =>
            {
                IDisposable? disposable = null;
                BytesRead += action;
                disposable = Disposable.Create(() => BytesRead -= action);
                return disposable!;
                async void action(object? self, BytesReadEventArgs args)
                {
                    try
                    {
                        await stream.WriteAsync(args.Buffer);
                    }
                    catch (Exception)
                    {
                        disposable?.Dispose();
                    }
                }
            }).ToArray();
        }
        public event EventHandler<BytesReadEventArgs>? BytesRead;
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
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                BytesRead?.Invoke(this, new BytesReadEventArgs(buffer));
            });
        }
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
