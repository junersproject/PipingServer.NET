using System;
using System.Diagnostics;
using System.Threading;
public static class DebugUtils
{
    public static CancellationTokenSource CreateTokenSource(TimeSpan delay)
    {
        return Debugger.IsAttached
            ? new CancellationTokenSource()
            : new CancellationTokenSource(delay);
    }
}
