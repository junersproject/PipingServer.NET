using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Piping.Server.Core;
using Piping.Server.Mvc.Binder;

namespace Piping.Server.Mvc.Models
{
    [ModelBinder(typeof(SendBinder))]
    public class SendData : IDisposable
    {
        public SendData(RequestKey Key) => this.Key = Key;
        public RequestKey Key { get; }
        private Task<(IHeaderDictionary Header, Stream Stream)>? Result;
        public void SetResult(Task<(IHeaderDictionary Header, Stream Stream)> Result)
            => this.Result = Result;
        public Task<(IHeaderDictionary Header, Stream Stream)> GetResultAsync()
            => Result ?? throw new InvalidOperationException("no set Result");
        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Result?.Dispose();
                    Result = null;
                }
                disposedValue = true;
            }
        }

        ~SendData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
