using System;

namespace Piping.Core.Pipes
{
    public class PipingOptions
    {
        /// <summary>
        /// Waiting Timeout Value.
        /// </summary>
        public TimeSpan? WatingTimeout { get; set; } = null;
    }
}
