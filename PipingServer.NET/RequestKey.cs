using System;
using System.Web;

namespace Piping
{
    public readonly struct RequestKey
    {
        public readonly string LocalPath;
        public readonly int Receivers;
        public RequestKey(string relativeUri) : this (new Uri(relativeUri, UriKind.Relative)) { }
        public RequestKey(Uri relativeUri)
        {
            var Collection = HttpUtility.ParseQueryString(relativeUri.Query);
            var n = Collection.Get("n");
            Receivers = n != null && uint.TryParse(n, out var _n) ? (int)_n : 1;
            LocalPath = relativeUri.LocalPath;
        }
        public override int GetHashCode() => LocalPath.GetHashCode();
        public override bool Equals(object obj)
        {
            return obj is RequestKey other ? other.LocalPath == LocalPath : false;
        }
    }
}
