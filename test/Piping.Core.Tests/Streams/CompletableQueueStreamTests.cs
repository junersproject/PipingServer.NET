using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping.Core.Streams;
using static DebugUtils;

namespace Piping.Streams.Tests
{
    [TestClass]
    public class CompletableQueueStreamTests
    {
        [TestMethod, TestCategory("ShortTime")]
        [Ignore]
        public void StoppedReadTest1()
        {
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(100);
            using var stream = new CompletableQueueStream();
            var Tasks = new[] { Task.Run(() => stream.Read(buffer, 0, buffer.Length)) };
            using var TokenSource = new CancellationTokenSource(Time);
            Assert.ThrowsException<OperationCanceledException>(
                () =>
                {
                    try
                    {
                        Task.WaitAny(Tasks, TokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Trace.WriteLine(e);
                        throw;
                    }
                });
        }
        [TestMethod, TestCategory("ShortTime")]
        [Ignore]
        public void StartReadTest1()
        {
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(100);
            using var stream = new CompletableQueueStream();
            stream.Write(data, 0, data.Length);
            int ReadBytes = 0;
            using (var TokenSource = new CancellationTokenSource(Time))
                Task.WaitAny(new[] { Task.Run(() => ReadBytes = stream.Read(buffer, 0, buffer.Length)) }, TokenSource.Token);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, ReadBytes);
            var Tasks = new[] { Task.Run(() => stream.Read(buffer, 0, buffer.Length)) };
            using (var TokenSource = new CancellationTokenSource(Time))
                Assert.ThrowsException<OperationCanceledException>(
                () => Task.WaitAny(Tasks, TokenSource.Token));
            stream.Write(data, 0, data.Length);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = stream.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, ReadBytes);
        }
        [TestMethod, TestCategory("ShortTime")]
        [Ignore]
        public void StoppedReadTest2()
        {
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(100);
            using var stream = new CompletableQueueStream();
            using var TokenSource = CreateTokenSource(Time);
            var Tasks = new[] { Task.Run(() => stream.Read(buffer)) };
            Assert.ThrowsException<OperationCanceledException>(
                () =>
                {
                    try
                    {
                        Task.WaitAny(Tasks, TokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Trace.WriteLine(e);
                        throw;
                    }
                });
        }
        [TestMethod, TestCategory("ShortTime")]
        [Ignore]
        public void StartReadTest2()
        {
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(100);
            using var stream = new CompletableQueueStream();
            stream.Write(data);
            int ReadBytes = 0;
            using (var TokenSource = new CancellationTokenSource(Time))
                Task.WaitAny(new[] { Task.Run(() => ReadBytes = stream.Read(buffer)) }, TokenSource.Token);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = stream.Read(buffer);
            Assert.AreEqual(buffer.Length, ReadBytes);

            using (var TokenSource = new CancellationTokenSource(Time))
                Assert.ThrowsException<OperationCanceledException>(
                () => Task.WaitAny(new[] { Task.Run(() => stream.Read(buffer)) }, TokenSource.Token));
            stream.Write(data);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = stream.Read(buffer);
            Assert.AreEqual(buffer.Length, ReadBytes);
        }
        [TestMethod]
        public void WriteTest()
        {
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            using var stream = new CompletableQueueStream();
            stream.Write(data.AsSpan());
        }
        [TestMethod]
        public void WriteAndCompleteAddingTest()
        {
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            using var stream = new CompletableQueueStream();
            Assert.AreEqual(true, stream.CanWrite);
            stream.Write(data.AsSpan());
            stream.CompleteAdding();
            Assert.AreEqual(false, stream.CanWrite);
            Assert.ThrowsException<InvalidOperationException>(() => stream.Write(data.AsSpan()));
        }
        [TestMethod]
        public async Task WriteAsyncTest()
        {
            var Time = TimeSpan.FromMilliseconds(100);
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            using var stream = new CompletableQueueStream();
            using var TokenSource = new CancellationTokenSource(Time);
            await stream.WriteAsync(data, TokenSource.Token);
        }
        [TestMethod]
        public async Task WriteAsyncAndCompleteAddingTest()
        {
            var Time = TimeSpan.FromMilliseconds(100);
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            using var stream = new CompletableQueueStream();
            Assert.AreEqual(true, stream.CanWrite);
            using (var TokenSource = new CancellationTokenSource(Time))
                await stream.WriteAsync(data, TokenSource.Token);
            stream.CompleteAdding();
            Assert.AreEqual(false, stream.CanWrite);
            using (var TokenSource = new CancellationTokenSource(Time))
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await stream.WriteAsync(data, TokenSource.Token));
        }
        [TestMethod, TestCategory("ShortTime")]
        public void StoppedReadAsyncTest1()
        {
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(10);
            using var stream = new CompletableQueueStream();
            using var TokenSource = CreateTokenSource(Time);
            Assert.ThrowsExceptionAsync<OperationCanceledException>(
                async () => await stream.ReadAsync(buffer.AsMemory(), TokenSource.Token));
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task StartReadAsyncTest1()
        {
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(100);
            using var stream = new CompletableQueueStream();
            await stream.WriteAsync(data);
            int ReadBytes = 0;
            using (var TokenSource = new CancellationTokenSource(Time))
                ReadBytes = await stream.ReadAsync(buffer, TokenSource.Token);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = await stream.ReadAsync(buffer);
            Assert.AreEqual(buffer.Length, ReadBytes);
            using (var TokenSource = new CancellationTokenSource(Time))
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    async () => await stream.ReadAsync(buffer, TokenSource.Token));
            await stream.WriteAsync(data);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = await stream.ReadAsync(buffer);
            Assert.AreEqual(buffer.Length, ReadBytes);
        }
        [TestMethod, TestCategory("ShortTime")]
        public void StoppedReadAsyncTest2()
        {
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(10);
            using var stream = new CompletableQueueStream();
            using var TokenSource = CreateTokenSource(Time);
            Assert.ThrowsExceptionAsync<OperationCanceledException>(
                () => stream.ReadAsync(buffer, 0, buffer.Length, TokenSource.Token));
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task StartReadAsyncTest2()
        {
            var data = Enumerable.Range(0, 200).Select(v => (byte)v).ToArray();
            var buffer = new byte[100];
            var Time = TimeSpan.FromMilliseconds(100);
            using var stream = new CompletableQueueStream();
            await stream.WriteAsync(data, 0, data.Length);
            int ReadBytes = 0;
            using (var TokenSource = new CancellationTokenSource(Time))
                ReadBytes = await stream.ReadAsync(buffer, 0, buffer.Length, TokenSource.Token);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, ReadBytes);
            using (var TokenSource = new CancellationTokenSource(Time))
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    () => stream.ReadAsync(buffer, 0, buffer.Length, TokenSource.Token));
            await stream.WriteAsync(data, 0, data.Length);
            Assert.AreEqual(buffer.Length, ReadBytes);
            ReadBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            Assert.AreEqual(buffer.Length, ReadBytes);
        }
    }
}
