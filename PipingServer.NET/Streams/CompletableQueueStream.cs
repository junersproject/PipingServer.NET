using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Piping.Streams
{
    /// <summary>
    /// Completable queue stream.
    /// </summary>
    public class CompletableQueueStream : Stream
    {
        readonly bool isWritableMode = true;
        public static CompletableQueueStream Empty { get; } = new CompletableQueueStream(false);
        readonly Pipe data;
        public bool IsAddingCompleted { get; private set; } = false;
        public void CompleteAdding(){
            data.Writer.Complete();
            IsAddingCompleted = true;
        }
        public CompletableQueueStream() :this(PipeOptions.Default) { }
        public CompletableQueueStream(PipeOptions options) => data = new Pipe(options);
        private CompletableQueueStream(bool isWritableMode) : this(new PipeOptions()) => this.isWritableMode = isWritableMode;
        public override bool CanRead => isWritableMode;

        public override bool CanSeek => false;

        public override bool CanWrite => isWritableMode && !IsAddingCompleted;
        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            //noop
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken Token = default)
        {
            var Read = await data.Reader.ReadAsync(Token);
            var Sequence = Read.Buffer;
            if (Read.IsCompleted && Sequence.Length == 0)
                return 0;
            if (Sequence.Length > buffer.Length)
                Sequence = Sequence.Slice(0, buffer.Length);
            else if (Sequence.Length < buffer.Length)
                buffer = buffer.Slice(0, (int)Sequence.Length);
            Sequence.CopyTo(buffer.Span);
            if (Read.Buffer.Length > buffer.Length)
                data.Reader.AdvanceTo(Read.Buffer.Slice(buffer.Length).Start);
            else 
                data.Reader.AdvanceTo(Read.Buffer.End);
            return buffer.Length;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken Token)
            => await ReadAsync(buffer.AsMemory().Slice(offset, count), Token).ConfigureAwait(false);
        public override int Read(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan().Slice(offset, count));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanWrite)
                throw new InvalidOperationException();
            var result = await data.Writer.WriteAsync(buffer, cancellationToken);
            if (result.IsCanceled)
                throw new OperationCanceledException(cancellationToken);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).ConfigureAwait(false);
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!CanWrite)
                throw new InvalidOperationException();
            data.Writer.Write(buffer);
        }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan().Slice(offset, count));
        protected override void Dispose(bool disposing)
        {
            data.Writer.Complete(new ObjectDisposedException("Disposed"));
            base.Dispose(disposing);
        }
    }
}
