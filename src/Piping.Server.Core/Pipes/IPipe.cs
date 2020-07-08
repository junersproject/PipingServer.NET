namespace Piping.Server.Core.Pipes
{
    public interface IPipe
    {
        RequestKey Key { get; }
        PipeStatus Status { get; }
        bool IsRemovable { get; }
        int ReceiversCount { get; }
        PipeType Required { get; }
    }
}
