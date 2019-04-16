using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Piping
{
    public class Service : IService
    {
        public static void Configure(ServiceConfiguration config)
        {
            config.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true, HttpsGetEnabled = true, });
            config.EnableProtocol(new BasicHttpBinding{ TransferMode = TransferMode.Streamed });
            config.EnableProtocol(new BasicHttpsBinding{ TransferMode = TransferMode.Streamed });
            //config.EnableProtocol(new NetTcpBinding{ TransferMode = TransferMode.Streamed });
            config.AddServiceEndpoint(typeof(IService), new WebHttpBinding {
                TransferMode = TransferMode.Streamed,
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                ReceiveTimeout = TimeSpan.FromHours(1),
                SendTimeout = TimeSpan.FromHours(1),
            }, "basic");
        }
        public string PostUpload(Stream inputStream)
        {
            throw new NotImplementedException();
        }
        public string PutUpload(Stream inputStream)
        {
            throw new NotImplementedException();
        }
        public Stream Download(Stream inputStream)
        {
            throw new NotImplementedException();
        }
    }
}
