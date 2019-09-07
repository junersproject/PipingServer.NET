using System;
using System.Diagnostics;
using System.Threading;
internal static class DebugUtils
{
    public static CancellationTokenSource CreateTokenSource(TimeSpan delay)
    {
        return Debugger.IsAttached
            ? new CancellationTokenSource()
            : new CancellationTokenSource(delay);
    }
}
