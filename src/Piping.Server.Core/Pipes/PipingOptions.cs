using System;
using System.Text;

namespace Piping.Server.Core.Pipes
{
    public class PipingOptions
    {
        /// <summary>
        /// Waiting Timeout Value.
        /// </summary>
        public TimeSpan? WaitingTimeout { get; set; } = null;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public int BufferSize { get; set; } = 1024 * 4;
        public string? SenderResponseMessageContentType { get; set; } = null;
    }
}
