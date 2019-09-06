using System;
using System.Collections.Generic;
using System.Text;

namespace Piping.Core.Internal
{
    internal class Disposable : IDisposable
    {
        readonly Action Action;
        public static IDisposable Create(Action Action) => new Disposable(Action);
        private Disposable(Action Action) => this.Action = Action;

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
                        Action();
                    }
                    catch (Exception)
                    {

                    }
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

    }
}
