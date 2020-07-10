using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace PipingServer.Core.Streams
{
    /// <summary>
    /// Completable queue stream.
    /// </summary>
    public class PipelineStream : Stream
    {
        long length = 0;
        readonly bool isWritableMode = true;
        public static PipelineStream Empty { get; } = new PipelineStream(false);
        readonly Pipe Pipe;
        public bool IsAddingCompleted { get; private set; } = false;
        public void Reset() => Pipe.Reset();
        public void Complete(Exception? ex = null)
        {
            Pipe.Writer.Complete(ex);
            IsAddingCompleted = true;
        }
        public async ValueTask CompleteAsync(Exception? ex = null)
        {
            await Pipe.Writer.CompleteAsync(ex);
            IsAddingCompleted = true;
        }
        public PipelineStream() : this(PipeOptions.Default) { }
        public PipelineStream(PipeOptions options) => Pipe = new Pipe(options);
        private PipelineStream(bool isWritableMode) : this(new PipeOptions()) => this.isWritableMode = isWritableMode;


        public override long Position { get => throw new NotSupportedException(); set => throw new NotImplementedException(); }
        #region flush is noop
        public override void Flush()
        {
            _ = Pipe.Writer.FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        public override Task FlushAsync(CancellationToken cancellationToken) => Pipe.Writer.FlushAsync(cancellationToken).AsTask();
        #endregion
        #region read is support        
        public override bool CanRead => isWritableMode;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken Token = default)
        {
            var Read = await Pipe.Reader.ReadAsync(Token);
            var Sequence = Read.Buffer;
            if (Read.IsCompleted && Sequence.Length == 0)
                return 0;
            if (Sequence.Length > buffer.Length)
                Sequence = Sequence.Slice(0, buffer.Length);
            else if (Sequence.Length < buffer.Length)
                buffer = buffer.Slice(0, (int)Sequence.Length);
            Sequence.CopyTo(buffer.Span);
            if (Read.Buffer.Length > buffer.Length)
                Pipe.Reader.AdvanceTo(Read.Buffer.Slice(buffer.Length).Start);
            else
                Pipe.Reader.AdvanceTo(Read.Buffer.End);
            Interlocked.Add(ref length, -1 * buffer.Length);
            return buffer.Length;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken Token)
            => await ReadAsync(buffer.AsMemory().Slice(offset, count), Token).ConfigureAwait(false);
        public override int Read(Span<byte> buffer)
        {
            if (!Pipe.Reader.TryRead(out var Read))
                return 0;
            var Sequence = Read.Buffer;
            if (Read.IsCompleted && Sequence.Length == 0)
                return 0;
            if (Sequence.Length > buffer.Length)
                Sequence = Sequence.Slice(0, buffer.Length);
            else if (Sequence.Length < buffer.Length)
                buffer = buffer.Slice(0, (int)Sequence.Length);
            Sequence.CopyTo(buffer);
            if (Read.Buffer.Length > buffer.Length)
                Pipe.Reader.AdvanceTo(Read.Buffer.Slice(buffer.Length).Start);
            else
                Pipe.Reader.AdvanceTo(Read.Buffer.End);
            Interlocked.Add(ref length, -1 * buffer.Length);
            return buffer.Length;
        }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan().Slice(offset, count));
        public void CancelPendingRead() => Pipe.Reader.CancelPendingRead();
        #endregion
        #region seek is not support
        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        #endregion
        #region length is not support
        public override long Length => Interlocked.Read(ref length);
        public override void SetLength(long value) => throw new NotSupportedException();
        #endregion
        #region write is support
        public override bool CanWrite => isWritableMode && !IsAddingCompleted;
        public async ValueTask<FlushResult> WriteAndReturnFlushResultAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = await Pipe.Writer.WriteAsync(buffer, cancellationToken);
            Interlocked.Add(ref length, buffer.Length);
            return result;
        }
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = await WriteAndReturnFlushResultAsync(buffer, cancellationToken);
            if (result.IsCanceled)
                throw new OperationCanceledException(cancellationToken);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).ConfigureAwait(false);
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var Writer = Pipe.Writer;
            var Span = Writer.GetSpan(buffer.Length);
            buffer.CopyTo(Span);
            Writer.Advance(buffer.Length);
            Interlocked.Add(ref length, buffer.Length);
        }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan().Slice(offset, count));
        public void CancelPendingFlush() => Pipe.Writer.CancelPendingFlush();
        #endregion
        protected override void Dispose(bool disposing)
        {
            Pipe.Writer.Complete();
            base.Dispose(disposing);
        }
        public override async ValueTask DisposeAsync()
        {
            await Pipe.Writer.CompleteAsync();
            await base.DisposeAsync();
        }
    }
}
