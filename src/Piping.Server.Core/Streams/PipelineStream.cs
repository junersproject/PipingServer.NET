using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Piping.Server.Core.Streams
{
    /// <summary>
    /// Completable queue stream.
    /// </summary>
    public class PipelineStream : Stream
    {
        readonly bool isWritableMode = true;
        public static PipelineStream Empty { get; } = new PipelineStream(false);
        readonly Pipe data;
        public bool IsAddingCompleted { get; private set; } = false;
        public void Complete(Exception? ex = null)
        {
            data.Writer.Complete(ex);
            IsAddingCompleted = true;
        }
        public async ValueTask CompleteAsync(Exception? ex = null)
        {
            await data.Writer.CompleteAsync(ex);
            IsAddingCompleted = true;
        }
        public PipelineStream() : this(PipeOptions.Default) { }
        public PipelineStream(PipeOptions options) => data = new Pipe(options);
        private PipelineStream(bool isWritableMode) : this(new PipeOptions()) => this.isWritableMode = isWritableMode;


        public override long Position { get => throw new NotSupportedException(); set => throw new NotImplementedException(); }
        #region flush is noop
        public override void Flush() {
            _ = data.Writer.FlushAsync().ConfigureAwait(false).GetAwaiter().GetResult(); 
        }
        public override Task FlushAsync(CancellationToken cancellationToken) => data.Writer.FlushAsync(cancellationToken).AsTask();
        #endregion
        #region read is support        
        public override bool CanRead => isWritableMode;
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
            if (!data.Reader.TryRead(out var Read))
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
                data.Reader.AdvanceTo(Read.Buffer.Slice(buffer.Length).Start);
            else
                data.Reader.AdvanceTo(Read.Buffer.End);
            return buffer.Length;
        }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan().Slice(offset, count));
        #endregion
        #region seek is not support
        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        #endregion
        #region length is not support
        public override long Length => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        #endregion
        #region write is support
        public override bool CanWrite => isWritableMode && !IsAddingCompleted;
        
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = await data.Writer.WriteAsync(buffer, cancellationToken);
            if (result.IsCanceled)
                throw new OperationCanceledException(cancellationToken);
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).ConfigureAwait(false);
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var Writer = data.Writer;
            var Span = Writer.GetSpan(buffer.Length);
            buffer.CopyTo(Span);
            Writer.Advance(buffer.Length);
        }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan().Slice(offset, count));
        #endregion
        protected override void Dispose(bool disposing)
        {
            data.Writer.Complete(new ObjectDisposedException("Disposed"));
            base.Dispose(disposing);
        }
    }
}
