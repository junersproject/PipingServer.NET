using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Piping.Server.Core.Pipes
{
    public static class ReadOnlyPipeExtensions
    {
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
                        yield return HttpMethods.Post;
                        yield return HttpMethods.Put;
                    }
                    else if ((Required & PipeType.Receiver) > 0)
                    {
                        yield return HttpMethods.Get;
                    }
                    if ((Required & PipeType.Sender) == 0)
                    {
                        yield return HttpMethods.Head;
                    }
                }
            } else {
                yield return HttpMethods.Get;
                yield return HttpMethods.Post;
                yield return HttpMethods.Put;
            }
            yield return HttpMethods.Options;
        }
        public static async ValueTask<IEnumerable<string>> GetOptionMethodsAsync(this IPipingStore Store, RequestKey Key, CancellationToken Token = default)
            => (await (Store ?? throw new ArgumentNullException(nameof(Store)))
                .GetAsync(Key, Token)
                ).ToOptionMethods();
    }
}
