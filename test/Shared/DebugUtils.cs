using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Options;

public static class DebugUtils
{
    public static CancellationTokenSource CreateTokenSource(TimeSpan delay)
    {
        return Debugger.IsAttached
            ? new CancellationTokenSource()
            : new CancellationTokenSource(delay);
    }

    public static IOptions<IOption> OptionsCreate<IOption>(IOption option) where IOption : class, new()
        => Options.Create(option);
}
