using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;

namespace Piping
{
    [ServiceContract(SessionMode = SessionMode.NotAllowed, Namespace = "piping-server")]
    public interface IService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method ="*", BodyStyle =WebMessageBodyStyle.Bare, RequestFormat =WebMessageFormat.Json,ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> DefaultAsync(Stream InputStream);

        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> GetDownloadAsync();

        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "POST", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> PostUploadAsync(Stream InputStream);

        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "PUT", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> PutUploadAsync(Stream InputStream);

        [OperationContract]
        [WebInvoke(UriTemplate = "help", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> GetHelpAsync();
        [OperationContract]
        [WebInvoke(UriTemplate = "version", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> GetVersionAsync();
        [OperationContract]
        [WebInvoke(UriTemplate = "favicon.ico", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> GetFaviconAsync();
        [OperationContract]
        [WebInvoke(UriTemplate = "robots.txt", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> GetRobotsAsync();
        [OperationContract]
        [WebInvoke(UriTemplate = "*", Method = "OPTIONS", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Stream> GetOptionsAsync();
    
    }
}
