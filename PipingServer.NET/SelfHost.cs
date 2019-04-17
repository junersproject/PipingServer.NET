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

        public void Dispose()
        {
            try
            {
                Host?.Close();
            }catch(Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e);
            }
            Host = null;
        }
    }
}
