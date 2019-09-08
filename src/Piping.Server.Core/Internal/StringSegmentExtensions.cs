using Microsoft.Extensions.Primitives;

namespace Piping.Server.Core.Internal
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
