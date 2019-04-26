using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Piping
{
    /// <summary>
    /// Output portion of CacheStream. Streams bytes from a queue of buffers.
    /// Thread safe.
    /// </summary>
    /// <remarks>https://stackoverflow.com/questions/3202594/stream-to-file-while-returning-a-wcf-stream</remarks>
    public class CacheOutputStream : Stream
    {
        private volatile int position;
        private volatile int length;
        private volatile bool sourceIsClosed;
        // No Deque<T> in the BCL yet, but LinkedList is more or less the same.
        private readonly LinkedList<byte[]> buffers = new LinkedList<byte[]>();
        public CacheOutputStream(CacheStream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            stream.BytesRead += (o, e) => AddToQueue(e.Buffer);
            stream.Closing += (o, e) => sourceIsClosed = true;
        }
        private void AddToQueue(byte[] buffer)
        {
            if (buffer.Length == 0)
                return;
            lock (buffers)
            {
                buffers.AddLast(buffer);
                length += buffer.Length;
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            bool noMoreBuffersAvailable = HasNoMoreBuffersAvailable();
            // Guard clause - closed and nothing more to write.
            if (noMoreBuffersAvailable && sourceIsClosed)
                return 0;
            if (noMoreBuffersAvailable)
            {
                // Not closed yet! Block infinitely until we get closed or have some data.
                while (HasNoMoreBuffersAvailable())
                {
                    if (sourceIsClosed)
                        return 0;
                    Thread.Sleep(TimeSpan.FromMilliseconds(50));
                }
            }
            byte[] currentBuffer = GetCurrentBuffer();
            int numberOfBytesRead = DoRead(buffer, count, currentBuffer, offset);
            PutLeftoverBytesAtFrontOfQueue(currentBuffer, numberOfBytesRead);
            return numberOfBytesRead;
        }
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null) throw new ArgumentNullException("buffer");
            bool noMoreBuffersAvailable = HasNoMoreBuffersAvailable();
            // Guard clause - closed and nothing more to write.
            if (noMoreBuffersAvailable && sourceIsClosed)
                return 0;
            if (noMoreBuffersAvailable)
            {
                // Not closed yet! Block infinitely until we get closed or have some data.
                while (HasNoMoreBuffersAvailable())
                {
                    if (sourceIsClosed)
                        return 0;
                    await Task.Delay(TimeSpan.FromMilliseconds(50));
                }
            }
            byte[] currentBuffer = GetCurrentBuffer();
            int numberOfBytesRead = DoRead(buffer, count, currentBuffer, offset);
            PutLeftoverBytesAtFrontOfQueue(currentBuffer, numberOfBytesRead);
            return numberOfBytesRead;
        }
        // Check if caller didn't have enough space to fit the buffer.
        // Put the remaining bytes at the front of the queue.
        private void PutLeftoverBytesAtFrontOfQueue(byte[] currentBuffer, int numberOfBytesRead)
        {
            if (currentBuffer == null) throw new ArgumentNullException("currentBuffer");
            if (numberOfBytesRead == currentBuffer.Length)
                return; // Clean read!
            var remainingBuffer = new byte[currentBuffer.Length - numberOfBytesRead];
            Array.Copy(currentBuffer, numberOfBytesRead, remainingBuffer, 0, remainingBuffer.Length);
            lock (buffers)
                buffers.AddFirst(remainingBuffer);
        }
        private int DoRead(byte[] buffer, int count, byte[] currentBuffer, int offset)
        {
            int maxNumberOfBytesWeCanWrite = Math.Min(count, currentBuffer.Length);
            Array.Copy(currentBuffer, 0, buffer, offset, maxNumberOfBytesWeCanWrite);
            position += maxNumberOfBytesWeCanWrite;
            return maxNumberOfBytesWeCanWrite;
        }
        private byte[] GetCurrentBuffer()
        {
            byte[] currentBuffer;
            lock (buffers)
            {
                currentBuffer = buffers.First.Value;
                buffers.RemoveFirst();
            }
            return currentBuffer;
        }
        private bool HasNoMoreBuffersAvailable()
        {
            lock (buffers)
                return buffers.Count == 0;
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin)
            => throw new InvalidOperationException("Cannot seek in CachingStream.");
        public override void SetLength(long value)
            => throw new InvalidOperationException("Cannot set length in CachingStream.");
        public override void Write(byte[] buffer, int offset, int count)
            => throw new InvalidOperationException("Cannot write in a CachingStream.");
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position
        {
            get { return position; }
            set { throw new InvalidOperationException("Cannot set position in CachingStream."); }
        }
    }
}
