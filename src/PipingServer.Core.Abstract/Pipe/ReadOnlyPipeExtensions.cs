using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PipingServer.Core.Pipes
{
    public static class ReadOnlyPipeExtensions
    {
        const string POST = "POST";
        const string PUT = "PUT";
        const string GET = "GET";
        const string HEAD = "HEAD";
        const string OPTIONS = "OPTIONS";
        public static IEnumerable<string> ToOptionMethods(this IReadOnlyPipe? Pipe)
        {
            if (Pipe is IReadOnlyPipe && Pipe.Status != PipeStatus.None)
            {
                if (Pipe.Status == PipeStatus.Dispose)
                {
                    // noop
                }
                else
                {
                    var Required = Pipe.Required;
                    if ((Required & PipeType.Sender) > 0)
                    {
                        yield return POST;
                        yield return PUT;
                    }
                    else if ((Required & PipeType.Receiver) > 0)
                    {
                        yield return GET;
                    }
                    if ((Required & PipeType.Sender) == 0)
                    {
                        yield return HEAD;
                    }
                }
            }
            else
            {
                yield return GET;
                yield return POST;
                yield return PUT;
            }
            yield return OPTIONS;
        }
        public static async ValueTask<IEnumerable<string>> GetOptionMethodsAsync(this IPipingStore Store, RequestKey Key, CancellationToken Token = default)
            => (await (Store ?? throw new ArgumentNullException(nameof(Store)))
                .GetAsync(Key, Token)
                ).ToOptionMethods();
    }
}
