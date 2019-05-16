using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;

namespace Piping
{
    public class CloseRegisterStream : Stream
    {
        private Stream origin;
        
        private event EventHandler closed;
        public event EventHandler Closed {
            add {
                if (isDisposed)
                {
                    value.Invoke(this, new EventArgs());
                }
                else
                    closed += value;
            }
            remove => closed -= value;
        }
        public CloseRegisterStream(Stream origin) => this.origin = origin;
        public override bool CanRead => origin.CanRead;

        public override bool CanSeek => origin.CanSeek;

        public override bool CanWrite => origin.CanWrite;

        public override long Length => origin.Length;

        public override long Position { get => origin.Position; set => origin.Position = value; }

        public override void Flush() => origin.Flush();

        public override int Read(byte[] buffer, int offset, int count) => origin.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => this.origin.Seek(offset, origin);

        public override void SetLength(long value) => origin.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) => origin.Write(buffer, offset, count);
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => origin.BeginRead(buffer, offset, count, callback, state);
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            => origin.BeginWrite(buffer, offset, count, callback, state);
        public override bool CanTimeout => origin.CanTimeout;
        public override void Close()
        {
            FireEventsAndRelease();
            base.Close();
            origin.Close();
        }
        bool isDisposed = false;
        protected void FireEventsAndRelease()
        {
            if (isDisposed)
                return;
            try
            {
                closed.Invoke(this, new EventArgs());
            }
            catch { }
            foreach (var closedHandler in closed.GetInvocationList().Cast<EventHandler>())
                closed -= closedHandler;

            isDisposed = true;
        }
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => origin.CopyToAsync(destination, bufferSize, cancellationToken);
        public override ObjRef CreateObjRef(Type requestedType) => origin.CreateObjRef(requestedType);
        public override int EndRead(IAsyncResult asyncResult) => origin.EndRead(asyncResult);
        public override void EndWrite(IAsyncResult asyncResult) => origin.EndWrite(asyncResult);
        public override bool Equals(object obj) => origin.Equals(obj);
        public override Task FlushAsync(CancellationToken cancellationToken) => origin.FlushAsync(cancellationToken);
        public override int GetHashCode() => origin.GetHashCode();
        public override object InitializeLifetimeService() => origin.InitializeLifetimeService();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => origin.ReadAsync(buffer, offset, count, cancellationToken);
        public override int ReadByte() => origin.ReadByte();
        public override int ReadTimeout { get => origin.ReadTimeout; set => origin.ReadTimeout = value; }
        public override int WriteTimeout { get => origin.WriteTimeout; set => origin.WriteTimeout = value; }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => origin.WriteAsync(buffer, offset, count, cancellationToken);
        public override void WriteByte(byte value) => origin.WriteByte(value);
        public override string ToString() => origin.ToString();
        protected override void Dispose(bool disposing)
        {
            FireEventsAndRelease();
            base.Dispose(disposing);
            origin.Dispose();
        }
    }
}
