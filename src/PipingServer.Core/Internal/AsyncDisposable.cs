using System;
using System.Threading.Tasks;

namespace PipingServer.Core.Internal
{
    internal class AsyncDisposable : IAsyncDisposable
    {
        readonly Func<ValueTask>? ValueAction;
        readonly Func<Task>? Action;
        AsyncDisposable(Func<ValueTask> Action) => ValueAction = Action;
        AsyncDisposable(Func<Task> Action) => this.Action = Action;
        public static IAsyncDisposable Create(Func<ValueTask> Action) => new AsyncDisposable(Action);
        public static IAsyncDisposable Create(Func<Task> Action) => new AsyncDisposable(Action);
        #region IAsyncDisposable Support
        /// <summary>
        /// 重複する呼び出しの検出
        /// </summary>
        private bool disposedValue = false;
        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                disposedValue = true;
                if (ValueAction is Func<ValueTask>)
                    await ValueAction();
                else if (Action is Func<Task>)
                    await Action();
            }
        }
        #endregion
    }
}
