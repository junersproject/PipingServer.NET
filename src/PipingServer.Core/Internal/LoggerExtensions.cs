using System;
using Microsoft.Extensions.Logging;

namespace PipingServer.Core.Internal
{
    internal static class LoggerExtensions
    {
        public static IDisposable? LogScope(this ILogger? Logger, LogLevel LogLevel, string Prefix = "", string Start = "START", string Stop = "STOP", string Error = "ERROR")
        {
            if (Logger == null || Logger.IsEnabled(LogLevel))
                return null;
            var Stoped = Disposable.Create(() => Logger.LogDebug(Prefix + Stop));
            try
            {
                Logger.Log(LogLevel, Prefix + Start);
                return Stoped;
            }
            catch (Exception e)
            {
                Logger.LogError(e, Prefix + Error);
                Stoped.Dispose();
                throw;
            }
        }
        public static IDisposable? LogDebugScope(this ILogger? Logger, string Prefix = "", string Start = "START", string Stop = "STOP", string Error = "ERROR") => Logger.LogScope(LogLevel.Debug, Prefix, Start, Stop, Error);
        public static IDisposable? LogInformationScope(this ILogger? Logger, string Prefix = "", string Start = "START", string Stop = "STOP", string Error = "ERROR") => Logger.LogScope(LogLevel.Information, Prefix, Start, Stop, Error);
        public static IDisposable? LogTraceScope(this ILogger? Logger, string Prefix = "", string Start = "START", string Stop = "STOP", string Error = "ERROR") => Logger.LogScope(LogLevel.Trace, Prefix, Start, Stop, Error);
    }
}
