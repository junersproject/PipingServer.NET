using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Piping
{
    internal class SelfHost : IDisposable
    {
        ServiceHost Host;

        public void Open(params Uri[] baseAddress)
        {
            if (Host != null)
                throw new InvalidOperationException("Opend.");
            Host = new ServiceHost(typeof(Service), baseAddress);
            Host.Open();
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                try
                {
                    Host?.Close();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.WriteLine(e);
                }
                Host = null;
                disposedValue = true;
            }
        }
        ~SelfHost() => Dispose(false);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
