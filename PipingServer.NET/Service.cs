using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.ServiceModel.Web;

namespace Piping
{
    [AspNetCompatibilityRequirements(RequirementsMode =AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    [ServiceContract(SessionMode= SessionMode.NotAllowed)]
    public class Service : IService
    {
        /// <summary>
        /// デフォルト設定の反映
        /// </summary>
        /// <param name="config"></param>
        public static void Configure(ServiceConfiguration config)
        {
            var HttpGetEnabled = false;
            var HttpsGetEnabled = false;
            var TransferMode = System.ServiceModel.TransferMode.Streamed;
            var SendTimeout = TimeSpan.FromHours(1);
            var OpenTimeout = TimeSpan.FromHours(1);
            var CloseTimeout = TimeSpan.FromHours(1);
            var MaxBufferSize = int.MaxValue;
            var MaxReceivedMessageSize = int.MaxValue;
            foreach (var address in config.BaseAddresses)
                if (address.Scheme == "http")
                    HttpGetEnabled = true;
                else if (address.Scheme == "https")
                    HttpsGetEnabled = true;
            config.Description.Behaviors.Add(new ServiceMetadataBehavior
            {
                HttpGetEnabled = HttpGetEnabled,
                HttpsGetEnabled = HttpsGetEnabled,
            });
            config.AddServiceEndpoint(typeof(IService), new WebHttpBinding
            {
                TransferMode = TransferMode,
                SendTimeout = SendTimeout,
                OpenTimeout = OpenTimeout,
                CloseTimeout = CloseTimeout,
                MaxBufferSize = MaxBufferSize,
                MaxReceivedMessageSize = MaxReceivedMessageSize,
            }, "").EndpointBehaviors.Add(new WebHttpBehavior());
        }
        /// <summary>
        /// エントリーポイント
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        [OperationBehavior(ReleaseInstanceMode = ReleaseInstanceMode.None)]
        public Stream Default(Stream inputStream)
        {
            var IncomingRequest = WebOperationContext.Current.IncomingRequest;
            var Method = IncomingRequest.Method;
            var RequestUri = IncomingRequest.UriTemplateMatch.RequestUri;
            //WebOperationContext.Current.OutgoingResponse
            throw new NotImplementedException();
        }
        internal Stream 
    }
}
