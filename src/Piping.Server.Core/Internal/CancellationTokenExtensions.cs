using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Piping.Server.Core.Internal
{
    public static class CancellationTokenExtensions
    {
        public static Task AsTask(this CancellationToken? cancellationToken)
        {
            if (cancellationToken == null)
                return Task.CompletedTask;
            else
                return AsTask((CancellationToken)cancellationToken);
        }

        public static Task AsTask(this CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            else
            {
                var taskCompletionSource = new TaskCompletionSource<bool>();
                cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
                return taskCompletionSource.Task;
            }
        }
    }
}
