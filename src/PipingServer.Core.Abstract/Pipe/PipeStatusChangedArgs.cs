using Microsoft.AspNetCore.Http;

namespace PipingServer.Core.Pipes
{
    public class PipeStatusChangedArgs : IPipe
    {
        public PipeStatusChangedArgs(IPipe Pipe, IHeaderDictionary? Headers = null)
            => ((Key, Status, IsRemovable, ReceiversCount, Required), this.Headers) = (Pipe, Headers);
        public PipeStatusChangedArgs((RequestKey Key, PipeStatus Status, bool IsRemovable, int ReceiversCount, PipeType Required) Pipe)
            => (Key, Status, IsRemovable, ReceiversCount, Required) = Pipe;
        public RequestKey Key { get; }
        public PipeStatus Status { get; } = PipeStatus.Wait;

        public bool IsRemovable { get; }

        public int ReceiversCount { get; }
        public PipeType Required { get; }
        public IHeaderDictionary? Headers { get; }

    }
}
