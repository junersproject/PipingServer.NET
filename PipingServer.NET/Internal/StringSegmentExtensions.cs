using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Piping.Internal
{
    public static class StringSegmentExtensions
    {
        public static bool IsNullOrWhiteSpace(this in StringSegment segment)
        {
            if (segment == StringSegment.Empty)
                return true;
            foreach (var s in segment.AsSpan())
                if (!char.IsWhiteSpace(s))
                    return false;
            return true;
        }
        public static bool IsNullOrEmpty(this in StringSegment segment)
        {
            return segment == StringSegment.Empty || segment.Length == 0;
        }
    }
}
