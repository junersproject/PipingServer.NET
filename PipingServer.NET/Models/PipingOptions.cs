using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Piping.Models
{
    public class PipingOptions
    {
        /// <summary>
        /// Waiting Timeout Value.
        /// </summary>
        public TimeSpan? WatingTimeout { get; set; } = TimeSpan.FromHours(1);
    }
}
