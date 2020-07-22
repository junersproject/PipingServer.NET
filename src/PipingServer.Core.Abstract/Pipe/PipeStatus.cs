namespace PipingServer.Core.Pipes
{
    public enum PipeStatus : byte
    {
        Created = 0,
        Wait,
        Ready,
        ResponseStart,
        ResponseEnd,
        Canceled,
        Dispose,
    }
}
