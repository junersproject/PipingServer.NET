using System;
using System.Collections.Generic;
using System.Linq;

namespace Piping.Server.Core
{
    public readonly struct RequestKey
    {
        public readonly static RequestKey Empty;
        /// <summary>
        /// 相対パスの辻褄合わせ用
        /// </summary>
        const string PATH = "http://example.com/";
        /// <summary>
        /// パス
        /// </summary>
        public readonly string LocalPath;
        /// <summary>
        /// 送信先数
        /// </summary>
        public readonly int Receivers;
        public RequestKey(string relativeUri) : this(new Uri(PATH + relativeUri.TrimStart('/').ToLower())) { }
        public RequestKey(Uri relativeUri)
        {
            var Collection = QueryToDictionary(relativeUri.Query);
            Receivers = Collection.TryGetValue("n", out var _n) && int.TryParse(_n, out var __n) ? __n : 1;
            if (Receivers <= 0)
                throw new InvalidOperationException($"n should > 0, but n = ${Receivers}.");
            LocalPath = relativeUri.LocalPath.ToLower();
        }
        public override int GetHashCode() => LocalPath.GetHashCode();
        public override bool Equals(object? obj)
        {
            return obj is RequestKey other ? other.LocalPath == LocalPath : false;
        }
        public static IDictionary<string, string> QueryToDictionary(string Query)
        {
            var dic = new Dictionary<string, string>();
            if (!Query.Any())
                return dic;
            if (Query.IndexOf('?') == 0)
                Query = Query.Substring(1);
            foreach (var Value in Query?.Split('&') ?? Enumerable.Empty<string>())
            {
                var first = Value.IndexOf('=');
                var hasValue = first >= 0;
                var k = hasValue ? Value.Substring(0, first) : Value;
                var v = hasValue ? Value.Substring(first + 1) : string.Empty;
                dic.Add(k, v);
            }
            return dic;
        }
        public override string ToString()
            => $"{LocalPath}" + (Receivers <= 1 ? "" : $"?n={Receivers}");
    }
}
