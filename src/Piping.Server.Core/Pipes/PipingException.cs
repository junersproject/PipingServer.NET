using System;

namespace Piping.Server.Core.Pipes
{
    public class PipingException : InvalidOperationException
    {
        public IReadOnlyPipe Pipe { get; }
        public PipingException(IReadOnlyPipe Pipe) : base()
            => this.Pipe = Pipe;
        public PipingException(string Message, IReadOnlyPipe Pipe) : base(Message)
            => this.Pipe = Pipe;
    }
}
