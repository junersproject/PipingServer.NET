using System;

namespace Piping.Models
{
    public class PipingOptions
    {
        /// <summary>
        /// Waiting Timeout Value.
        /// </summary>
        public TimeSpan? WatingTimeout { get; set; } = null;
    }
}
