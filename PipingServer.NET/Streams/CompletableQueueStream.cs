using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Channels;
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
        readonly Channel<ReadOnlyMemory<byte>> data;
        ReadOnlyMemory<byte> _currentBlock = ReadOnlyMemory<byte>.Empty;
        int _currentBlockIndex = 0;
        public bool IsAddingCompleted { get; private set; } = false;
        public void CompleteAdding(){
            data.Writer.TryComplete();
            IsAddingCompleted = true;
        }
        public CompletableQueueStream() => data = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
        public CompletableQueueStream(int boundedCapacity) => data = Channel.CreateBounded<ReadOnlyMemory<byte>>(boundedCapacity);
        private CompletableQueueStream(bool isWritableMode) : this(1) => this.isWritableMode = isWritableMode;
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
            if (!CanRead)
                throw new InvalidOperationException();
            if (_currentBlock.IsEmpty || _currentBlockIndex == _currentBlock.Length)
                if (!await GetNextBlockAsync(Token).ConfigureAwait(false))
                    return 0;
            var minCount = Math.Min(buffer.Length, _currentBlock.Length - _currentBlockIndex);
            _currentBlock.Slice(_currentBlockIndex, minCount).CopyTo(buffer);
            _currentBlockIndex += minCount;
            return minCount;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken Token)
            => await ReadAsync(buffer.AsMemory().Slice(offset, count), Token).ConfigureAwait(false);
        public override int Read(Span<byte> buffer)
        {
            if (!CanRead)
                throw new InvalidOperationException();
            if (_currentBlock.IsEmpty || _currentBlockIndex == _currentBlock.Length)
                if (!GetNextBlockAsync().Result)
                    return 0;
            var minCount = Math.Min(buffer.Length, _currentBlock.Length - _currentBlockIndex);
            _currentBlock.Span.Slice(_currentBlockIndex, minCount).CopyTo(buffer);
            _currentBlockIndex += minCount;
            return minCount;
        }
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan().Slice(offset, count));
        /// <summary>
        /// Loads the next block in to <see cref="_currentBlock"/>
        /// </summary>
        /// <returns>True if the next block was retrieved.</returns>
        private async ValueTask<bool> GetNextBlockAsync(CancellationToken Token = default)
        {
            if (await data.Reader.WaitToReadAsync(Token))
            {
                if (!data.Reader.TryRead(out _currentBlock))
                    return false;
            }
            else
                return false;
            _currentBlockIndex = 0;
            return true;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        { 
            if (!CanWrite)
                throw new InvalidOperationException();
            var localArray = new byte[buffer.Length];
            if (await data.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                buffer.CopyTo(localArray);
                data.Writer.TryWrite(localArray);
            }
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken).ConfigureAwait(false);
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (!CanWrite)
                throw new InvalidOperationException();
            var localArray = new byte[buffer.Length];
            if (data.Writer.WaitToWriteAsync().Result)
            {
                buffer.CopyTo(localArray);
                data.Writer.TryWrite(localArray);
            }
        }
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan().Slice(offset, count));
        protected override void Dispose(bool disposing)
        {
            data.Writer.TryComplete(new ObjectDisposedException("Disposed"));
            base.Dispose(disposing);
        }
    }
}
