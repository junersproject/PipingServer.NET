using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace PipingServer.Core
{
    public readonly struct RequestKey : IEquatable<string>, IEquatable<RequestKey>
    {
        public static readonly RequestKey Empty;
        /// <summary>
        /// パス
        /// </summary>
        public readonly string Path;
        /// <summary>
        /// クエリ
        /// </summary>
        public readonly IQueryCollection Query;
        /// <summary>
        /// 送信先数
        /// </summary>
        public readonly int Receivers;
        public RequestKey(string Path, IQueryCollection Query)
        {
            this.Path = (Path).ToLower();
            this.Query = Query;
            Receivers = Query.TryGetValue("n", out var _n) && int.TryParse(_n, out var __n) ? __n : 1;
            if (Receivers <= 0)
                throw new InvalidOperationException($"n should > 0, but n = ${Receivers}.");
        }
        public override int GetHashCode() => Path.GetHashCode();
        public override bool Equals(object? obj) => obj is RequestKey other && other.Path == Path;
        public bool Equals(string? Path) => this.Path == Path;
        public bool Equals(RequestKey Key) => Equals(Key.Path);
        public override string ToString()
            => string.Join("", new string?[]{
                $"{Path}",
                (Receivers <= 1 ? null : $"?n={Receivers}")
            }.OfType<string>());
    }
}
