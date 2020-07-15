using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PipingServer.Core.Internal;
using static PipingServer.Core.Properties.Resources;

namespace PipingServer.Core.Streams
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
                PipeWriteByte += ByteWrite;
                PipeWrite += Write;
                PipeWriteAsync += WriteAsync;
                PipeFlush += Flush;
                PipeFlushAsync += FlushAsync;
                disposable = Disposable.Create(() =>
                {
                    PipeWrite -= Write;
                    PipeWriteAsync -= WriteAsync;
                    PipeFlush -= Flush;
                    PipeFlushAsync -= FlushAsync;
                });
                return disposable!;
                void ByteWrite(byte value)
                {
                    try
                    {
                        stream.WriteByte(value);
                    }
                    catch (Exception)
                    {
                        disposable?.Dispose();
                    }
                }
                void Write(in ReadOnlySpan<byte> span)
                {
                    try
                    {
                        stream.Write(span);
                    }
                    catch (Exception)
                    {
                        disposable?.Dispose();
                    }
                }
                async ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken Token)
                {
                    try
                    {
                        await stream.WriteAsync(memory, Token);
                    }
                    catch (Exception)
                    {
                        disposable?.Dispose();
                    }
                }
                void Flush()
                {
                    try
                    {
                        stream.Flush();
                    }
                    catch (Exception)
                    {
                        disposable?.Dispose();
                    }
                }
                async Task FlushAsync(CancellationToken cancellationToken)
                {
                    try
                    {
                        await stream.FlushAsync(cancellationToken);
                    }
                    catch (Exception)
                    {
                        disposable?.Dispose();
                    }
                }
            }).ToArray();
        }
        #region flush is support
        delegate void PipeFlushEventHandler();
        delegate Task PipeFlushAsyncEventHandler(CancellationToken Token);
        event PipeFlushEventHandler? PipeFlush;
        public override void Flush() => PipeFlush?.Invoke();
        event PipeFlushAsyncEventHandler? PipeFlushAsync;
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
#pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
            Task[] Tasks = PipeFlushAsync?.GetInvocationList()
                .OfType<PipeFlushAsyncEventHandler>()
                .Select(x => x.Invoke(cancellationToken))
                .OfType<ValueTask>().Select(v => v.AsTask()).ToArray();
#pragma warning restore CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
            if (0 < (uint)Tasks!.Length)
#pragma warning disable CS8604 // Null 参照引数の可能性があります。
                await Task.WhenAll(Tasks);
#pragma warning restore CS8604 // Null 参照引数の可能性があります。
        }
        #endregion
        #region seek is not support
        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin origin)
            => throw new InvalidOperationException(string.Format(CannotSeekIn, nameof(PipingStream)));
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        #endregion
        #region length is not support
        public override void SetLength(long value) => throw new NotSupportedException();
        public override long Length => throw new NotSupportedException();
        #endregion
        #region read is not support
        public override bool CanRead => false;
        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan().Slice(offset, count));
        public override int Read(Span<byte> buffer) => throw new NotSupportedException();
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => await ReadAsync(buffer.AsMemory().Slice(offset, count), cancellationToken);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public override int ReadByte() => throw new NotSupportedException();
        #endregion
        #region write is support
        delegate ValueTask PipeWriteAsyncEventHandler(ReadOnlyMemory<byte> memory, CancellationToken Token);
        delegate void PipeWriteEventHandler(in ReadOnlySpan<byte> span);
        delegate void PipeWriteByteEventHandler(byte value);
        public override bool CanWrite => true;
        event PipeWriteEventHandler? PipeWrite;
        event PipeWriteByteEventHandler? PipeWriteByte;
        public override void Write(ReadOnlySpan<byte> buffer) => PipeWrite?.Invoke(buffer);
        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan().Slice(offset, count));

        event PipeWriteAsyncEventHandler? PipeWriteAsync;
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
#pragma warning disable CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
            Task[] Tasks = PipeWriteAsync?.GetInvocationList()
                .OfType<PipeWriteAsyncEventHandler>()
                .Select(x => x.Invoke(buffer, cancellationToken))
                .OfType<ValueTask>().Select(v => v.AsTask()).ToArray();
#pragma warning restore CS8600 // Null リテラルまたは Null の可能性がある値を Null 非許容型に変換しています。
            if (0 < (uint)Tasks!.Length)
#pragma warning disable CS8604 // Null 参照引数の可能性があります。
                await Task.WhenAll(Tasks);
#pragma warning restore CS8604 // Null 参照引数の可能性があります。
        }
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => await WriteAsync(buffer.AsMemory().Slice(offset, count), cancellationToken);
        public override void WriteByte(byte value) => PipeWriteByte?.Invoke(value);
        #endregion
        #region copy to is not support
        public override void CopyTo(Stream destination, int bufferSize) => throw new NotSupportedException();
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();
        #endregion
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // event clear
            foreach (var disposable in Disposables)
                disposable.Dispose();
        }
        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            // event clear
            foreach (var disposable in Disposables)
                disposable.Dispose();
        }
    }
}
