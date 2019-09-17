namespace Piping.Server.Core.Pipes
{
    public class PipeStatusChangedArgs
    {
        public PipeStatusChangedArgs() : this(PipeStatus.Wait) { }
        public PipeStatusChangedArgs(PipeStatus Status) => this.Status = Status;
        public PipeStatus Status { get; } = PipeStatus.Wait;
    }
}
