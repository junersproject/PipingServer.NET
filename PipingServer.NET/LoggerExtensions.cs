using System;
using Microsoft.Extensions.Logging;

namespace Piping
{
    public static class LoggerExtensions
    {
        public static IDisposable BeginLogInformationScope<T>(this ILogger<T> logger, string message, string startSuffix = " START", string stopSuffix = " STOP")
        {
            logger.LogInformation(message + startSuffix);
            return Disposable.Create(() => logger.LogInformation(message + stopSuffix));
        }
        public static IDisposable BeginLogTraceScope<T>(this ILogger<T> logger, string message, string startSuffix = " START", string stopSuffix = " STOP")
        {
            logger.LogTrace(message + startSuffix);
            return Disposable.Create(() => logger.LogTrace(message + stopSuffix));
        }
    }
}
