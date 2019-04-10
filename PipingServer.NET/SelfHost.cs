using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Piping
{
    internal class SelfHost : IDisposable
    {
        public SelfHost() { }
        public bool HttpGetEnabled { get; set; } = false;
        public bool HttpsGetEnabled { get; set; } = false;
        public TransferMode TransferMode { get; } = TransferMode.Streamed;
        public PolicyVersion PolicyVersion { get; set; } = PolicyVersion.Policy15;
        ServiceHost Host;

        public void Open(params Uri[] baseAddress)
        {
            if (Host != null)
                throw new InvalidOperationException("Opend.");
            Host = new ServiceHost(typeof(Service), baseAddress)
            {
                Description = {
                    Behaviors =
                    {
                        new ServiceMetadataBehavior
                        {
                            
                            HttpGetEnabled = HttpGetEnabled,
                            HttpsGetEnabled = HttpsGetEnabled,
                            MetadataExporter =
                            {
                                PolicyVersion = PolicyVersion,
                            },
                            HttpsGetBinding = new BasicHttpBinding
                            {
                                TransferMode = TransferMode,
                                
                            },
                        },
                    },
                },
            };
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
