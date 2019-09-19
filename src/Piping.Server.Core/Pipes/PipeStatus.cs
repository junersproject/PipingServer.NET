namespace Piping.Server.Core.Pipes
{
    public enum PipeStatus : byte
    {
        None = 0,
        Wait,
        Ready,
        ResponseStart,
        Canceled,
        Dispose,
    }
}
