namespace Piping.Server.Core.Pipes
{
    public interface IPipe
    {
        public void Deconstruct(out RequestKey Key, out PipeStatus Status, out bool IsRemovable, out int ReceiversCount, out PipeType Required)
            => (Key, Status, IsRemovable, ReceiversCount, Required) = (this.Key, this.Status, this.IsRemovable, this.ReceiversCount, this.Required);
        RequestKey Key { get; }
        PipeStatus Status { get; }
        bool IsRemovable { get; }
        int ReceiversCount { get; }
        public PipeType Required => PipeType.Unuse;
    }
}
