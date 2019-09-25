using System;
using System.Collections.Generic;
using System.Text;

namespace Piping.Server.Core.Options
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
        public PipingOption Option = new PipingOption
        {
            Headers = {
                { "Access-Control-Allow-Origin", "*" },
                { "Access-Control-Allow-Methods", "GET, HEAD, POST, PUT, OPTIONS" },
                { "Access-Control-Allow-Headers", "Content-Type, Content-Disposition" },
                { "Access-Control-Max-Age", "86400" },
            }
        };
    }
    public class PipingOption
    {
        public IDictionary<string, string> Headers = new Dictionary<string, string>();
    }

}
