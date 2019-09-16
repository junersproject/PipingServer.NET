using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping.Server.Core.Pipes
{
    internal class SenderPipe : ISenderPipe
    {
        readonly Pipe Current;
        internal SenderPipe(Pipe Current)
            => this.Current = Current;
        public RequestKey Key => Current.Key;

        public PipeStatus Status => Current.Status;

        public bool IsRemovable => Current.IsRemovable;

        public int RequestedReceiversCount => Current.RequestedReceiversCount;

        public int ReceiversCount => Current.ReceiversCount;

        public event EventHandler? OnWaitTimeout
        {
            add => Current.OnWaitTimeout += value;
            remove => Current.OnWaitTimeout -= value;
        }
        public event PipeStatusChangeEventHandler? OnStatusChanged
        {
            add => Current.OnStatusChanged += value;
            remove => Current.OnStatusChanged -= value;
        }

        public ValueTask ReadyAsync(CancellationToken Token = default) => Current.ReadyAsync(Token);

        public async ValueTask SetHeadersAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, CancellationToken Token = default)
        {
            await Current.SetHeadersAsync(DataTask, Token);
        }

        public Task ConnectionAsync(Task<(IHeaderDictionary Headers, Stream Stream)> DataTask, ICompletableStream CompletableStream, CancellationToken Token = default)
        {
            throw new NotImplementedException();
        }
    }
}
