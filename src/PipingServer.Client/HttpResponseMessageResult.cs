using System.Net.Http;
using System;

namespace PipingServer.Client
{
    public class HttpResponseMessageResult : IDisposable
    {
        public HttpResponseMessage Message { get; }
        readonly IDisposable Disposable;
        private bool disposedValue;

        public HttpResponseMessageResult(HttpResponseMessage Message, IDisposable Disposable)
            => (this.Message, this.Disposable) = (Message, Disposable);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Message.Dispose();
                    Disposable.Dispose();
                }
                disposedValue = true;
            }
        }
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
