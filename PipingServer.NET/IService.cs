using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Piping
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebInvoke(Method = "POST")]
        string PostUpload(Stream InputStream);
        [OperationContract]
        [WebInvoke(Method = "PUT")]
        string PutUpload(Stream InputStream);
        [OperationContract]
        [WebInvoke(Method = "GET")]
        Stream Download(Stream inputStream);
    }
}
