using System;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Piping
{
    internal class SelfHost : IDisposable
    {
        public SelfHost() { }
        public bool HttpGetEnabled { get; set;}
        public bool HttpsGetEnabled { get; set; }
        ServiceHost Host;

        public void Open(params Uri[] baseAddress)
        {
            if (Host != null)
                throw new InvalidOperationException("Opend.");
            Host = new ServiceHost(typeof(Service), baseAddress) {
                Description = {
                    Behaviors =
                    {
                        new ServiceMetadataBehavior
                        {
                            HttpGetEnabled = true,
                            HttpsGetEnabled = true,
                            MetadataExporter =
                            {
                                PolicyVersion = PolicyVersion.Policy15,
                            },
                        },
                    },
                },
            };
            Host.Open();
        }

        public void Dispose()
        {
            Host?.Close();
            Host = null;
        }
    }
}
