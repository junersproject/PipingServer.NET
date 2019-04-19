using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Piping
{
    [ServiceContract(SessionMode = SessionMode.NotAllowed, Namespace = "piping-server")]
    public interface IService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method ="*", BodyStyle =WebMessageBodyStyle.Bare, RequestFormat =WebMessageFormat.Json,ResponseFormat = WebMessageFormat.Json)]
        Stream Default(Stream InputStream);

        [OperationContract]
        [WebInvoke(UriTemplate = "help", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream GetHelp();
        [OperationContract]
        [WebInvoke(UriTemplate = "version", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream GetVersion();
        [OperationContract]
        [WebInvoke(UriTemplate = "favicon.ico", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream GetFavicon();
        [OperationContract]
        [WebInvoke(UriTemplate = "robots.txt", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream GetRobots();
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "OPTIONS", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Stream GetOptions();
    
    }
}
