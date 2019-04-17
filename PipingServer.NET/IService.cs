using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Piping
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebInvoke(UriTemplate ="", Method ="*", BodyStyle =WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json)]
        Stream Default(Stream InputStream);
    }
}
