using System;
using Microsoft.Extensions.Logging;
using PipingServer.Mvc.Internal;

namespace PipingServer.Mvc
{
    public static class LoggerExtensions
    {
        public static IDisposable BeginLogDebugScope<T>(this ILogger<T> logger, string message, string startSuffix = " START", string stopSuffix = " STOP")
        {
            logger.LogDebug(message + startSuffix);
            return Disposable.Create(() => logger.LogInformation(message + stopSuffix));
        }
    }
}
