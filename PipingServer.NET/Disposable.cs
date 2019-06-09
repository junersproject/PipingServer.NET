using System;

namespace Piping
{
    internal class Disposable : IDisposable
    {
        Action? Action;
        Disposable(Action Action) => this.Action = Action;
        public static IDisposable Create(Action Action) => new Disposable(Action ?? throw new ArgumentNullException(nameof(Action)));

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        Action?.Invoke();
                    }
                    catch { }
                    Action = null;
                }
                disposedValue = true;
            }
        }

        ~Disposable()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
