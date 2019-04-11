using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Piping
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "{perKey}", Method = "POST")]
        string PostUpload(string perKey, Stream InputStream);
        [OperationContract]
        [WebInvoke(UriTemplate = "{perKey}", Method = "PUT")]
        string PutUpload(string perKey, Stream InputStream);
        [OperationContract]
        [WebInvoke(UriTemplate = "{perKey}", Method = "GET")]
        Stream Download(string perKey, Stream inputStream);
    }
}
