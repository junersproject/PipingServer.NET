namespace Piping.Server.Core.Pipes
{
    public enum PipeStatus : byte
    {
        None = 0,
        Wait,
        Ready,
        ResponseStart,
        ResponseEnd,
        Canceled,
        Dispose,
    }
}
