using System;
using System.Web;

namespace Piping
{
    public readonly struct RequestKey
    {
        const string PATH = "http://example.com/";
        public readonly string LocalPath;
        public readonly int Receivers;
        public RequestKey(string relativeUri) : this (new Uri(PATH + relativeUri.TrimStart('/'))) { }
        public RequestKey(Uri relativeUri)
        {
            var Collection = HttpUtility.ParseQueryString(relativeUri.Query);
            var n = Collection.Get("n");
            Receivers = n != null && int.TryParse(n, out var _n) ? _n : 1;
            LocalPath = relativeUri.LocalPath.ToLower();
        }
        public override int GetHashCode() => LocalPath.GetHashCode();
        public override bool Equals(object obj)
        {
            return obj is RequestKey other ? other.LocalPath == LocalPath : false;
        }
    }
}
