namespace Piping.Server.Core.Pipes
{
    public enum PipeStatus : byte
    {
        Wait = 0,
        Ready = 1,
        ResponseStart = 2,
        Canceled = 3,
        Dispose = 4,
    }
}
