using System;
using System.Collections.Generic;
using System.Text;

namespace Piping.Core.Pipes
{
    public class PipingOptions
    {
        /// <summary>
        /// Waiting Timeout Value.
        /// </summary>
        public TimeSpan? WatingTimeout { get; set; } = null;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
    }
}
