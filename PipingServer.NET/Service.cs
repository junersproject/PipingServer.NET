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
            config.Description.Behaviors.Add(new ServiceMetadataBehavior { HttpGetEnabled = true });
            config.EnableProtocol(new BasicHttpBinding{ TransferMode = TransferMode.Streamed });
            config.EnableProtocol(new BasicHttpsBinding{ TransferMode = TransferMode.Streamed });
            //config.EnableProtocol(new NetTcpBinding{ TransferMode = TransferMode.Streamed });
            config.AddServiceEndpoint(typeof(IService), new BasicHttpBinding(), "basic");
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
