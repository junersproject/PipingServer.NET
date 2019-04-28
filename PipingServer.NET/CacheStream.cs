using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piping
{
    /// <summary>
    /// A stream that, as it reads, makes those bytes available on an ouput
    /// stream. Thread safe.
    /// </summary>
    /// <remarks>https://stackoverflow.com/questions/3202594/stream-to-file-while-returning-a-wcf-stream</remarks>
    public class CacheStream : Stream
    {
        private readonly Stream stream;
        public CacheStream(Stream stream, int receivers = 1)
        {
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            OutputStreams = Enumerable.Range(0, receivers).Select(v => new CacheOutputStream(this)).ToArray();
        }
        public event EventHandler<BytesReadEventArgs> BytesRead;
        public event EventHandler Closing;
        public Stream[] OutputStreams { get; private set; }
        public override void Flush()
        {
            stream.Flush();
        }
        public override long Seek(long offset, SeekOrigin origin)
            => throw new InvalidOperationException("Cannot seek in CachingStream.");
        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int numberOfBytesRead = stream.Read(buffer, offset, count);
            if (numberOfBytesRead > 0)
                PipeToOutputStream(buffer, offset, numberOfBytesRead);
            return numberOfBytesRead;
        }
        private void PipeToOutputStream(byte[] buffer, int offset, int numberOfBytesRead)
        {
            var tmp = new byte[numberOfBytesRead];
            Array.Copy(buffer, offset, tmp, 0, numberOfBytesRead);
            BytesRead?.Invoke(this, new BytesReadEventArgs(tmp));
        }
        public override void Write(byte[] buffer, int offset, int count)
            => throw new InvalidOperationException("Cannot write in CachingStream.");
        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length=> stream.Length; 
        public override long Position
        {
            get { return stream.Position; }
            set { throw new InvalidOperationException("Cannot set position in CachingStream."); }
        }
        public override void Close()
        {
            Closing?.Invoke(this, EventArgs.Empty);
            base.Close();
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            foreach( var stream in OutputStreams)
                stream.Dispose();
        }
    }
}
