using System.IO;
using System.ServiceModel.Web;

namespace Piping
{
    public class ReqRes
    {
        public IncomingWebRequestContext Request = null;
        public Stream RequestStream = null; 
        public OutgoingWebResponseContext Response = null;
        public Stream ResponseStream = null;
        public void Deconstruct(out IncomingWebRequestContext Request, Stream RequestStream, OutgoingWebResponseContext Response, Stream ResponseStream)
            => (Request, RequestStream, Response, ResponseStream) = (this.Request, this.RequestStream, this.Response, this.ResponseStream);
    }
}
