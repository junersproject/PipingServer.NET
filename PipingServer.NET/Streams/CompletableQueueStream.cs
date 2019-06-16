using System;
using System.Collections.Concurrent;
using System.IO;
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
        readonly BlockingCollection<byte[]> data;
        byte[] _currentBlock = Array.Empty<byte>();
        public int BoundedCapacity => data.BoundedCapacity;
        int _currentBlockIndex = 0;
        public int BufferedWrites => data.Count;
        public bool IsAddingCompleted => data.IsAddingCompleted;
        public bool IsCompleted => data.IsCompleted;
        public void CompleteAdding() => data.CompleteAdding();
        public CompletableQueueStream() => data = new BlockingCollection<byte[]>();
        public CompletableQueueStream(int boundedCapacity) => data = new BlockingCollection<byte[]>(boundedCapacity);
        private CompletableQueueStream(bool isWritableMode) : this(1) => this.isWritableMode = isWritableMode;
        public override bool CanRead => isWritableMode;

        public override bool CanSeek => false;

        public override bool CanWrite => isWritableMode && !data.IsAddingCompleted;
        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            //noop
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new InvalidOperationException();
            if (_currentBlock.Length == 0 || _currentBlockIndex == _currentBlock.Length)
                if (!GetNextBlock())
                    return 0;
            int minCount = Math.Min(count, _currentBlock.Length - _currentBlockIndex);
            Array.Copy(_currentBlock, _currentBlockIndex, buffer, offset, minCount);
            _currentBlockIndex += minCount;
            return minCount;
        }

        /// <summary>
        /// Loads the next block in to <see cref="_currentBlock"/>
        /// </summary>
        /// <returns>True if the next block was retrieved.</returns>
        private bool GetNextBlock(CancellationToken Token = default)
        {
            if (!data.TryTake(out _currentBlock, Timeout.Infinite, Token))
            {
                if (data.IsCompleted)
                    return false;
                try
                {
                    _currentBlock = data.Take(Token);
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
            _currentBlockIndex = 0;
            return true;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new InvalidOperationException();
            var localArray = new byte[count];
            Array.Copy(buffer, offset, localArray, 0, count);
            data.Add(localArray);
        }
        protected override void Dispose(bool disposing)
        {
            data.Dispose();
            base.Dispose(disposing);
        }
    }
}
