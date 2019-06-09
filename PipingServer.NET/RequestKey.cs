using System;
using Microsoft.AspNetCore.WebUtilities;

namespace Piping
{
    public readonly struct RequestKey
    {
        const string PATH = "http://example.com/";
        public readonly string LocalPath;
        public readonly int Receivers;
        public RequestKey(string relativeUri) : this(new Uri(PATH + relativeUri.TrimStart('/'))) { }
        public RequestKey(Uri relativeUri)
        {
            var Collection = QueryHelpers.ParseQuery(relativeUri.Query);
            Receivers = Collection.TryGetValue("n", out var _n) && int.TryParse(_n, out var __n) ? __n : 1;
            LocalPath = relativeUri.LocalPath.ToLower();
        }
        public override int GetHashCode() => LocalPath.GetHashCode();
        public override bool Equals(object obj)
        {
            return obj is RequestKey other ? other.LocalPath == LocalPath : false;
        }
        public override string ToString()
            => $"{LocalPath}?n={Receivers}";
    }
}
