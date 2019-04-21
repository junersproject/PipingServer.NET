using System;
using System.Web;

namespace Piping
{
    public readonly struct RequestKey
    {
        readonly string absolutePath;
        readonly int Receivers;
        public RequestKey(Uri relativeUri)
        {
            var Collection = HttpUtility.ParseQueryString(relativeUri.Query);
            var n = Collection.Get("n");
            Receivers = n != null && uint.TryParse(n, out var _n) ? (int)_n : 1;
            absolutePath = relativeUri.AbsolutePath;
        }
        public override int GetHashCode() => absolutePath.GetHashCode();
    }
}
