using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Piping.Server.Core.Pipes
{
    public static class CompletableStreamExtensions
    {
        public static void SetHeaders(this IEnumerable<ICompletableStream> Responses, IHeaderDictionary Headers)
        {
            foreach (var r in Responses)
                if (r.Headers is IHeaderDictionary _Headers)
                    foreach (var kv in Headers)
                        if (!_Headers.TryGetValue(kv.Key, out _))
                            _Headers[kv.Key] = kv.Value;
        }
    }
}
