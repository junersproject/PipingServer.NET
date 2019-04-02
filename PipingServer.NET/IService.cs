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
        Stream Pipe(string perKey, Stream InputStream);
    }
}
