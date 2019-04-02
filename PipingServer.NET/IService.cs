using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Piping
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "{perKey}")]
        string Upload(string perKey, Stream InputStream);
        [OperationContract]
        [WebGet(UriTemplate = "{perKey}")]
        Stream Download(string perKey);
    }
}
