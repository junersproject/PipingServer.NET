using System.IO;
using System.ServiceModel.Web;

namespace Piping
{
    public class ReqRes
    {
        public WebOperationContext Context { get; set; }
        public Stream RequestStream = null; 
        public Stream ResponseStream = null;
        public void Deconstruct(out IncomingWebRequestContext Request, Stream RequestStream, OutgoingWebResponseContext Response, Stream ResponseStream)
            => (Request, RequestStream, Response, ResponseStream) = (this.Context.IncomingRequest, this.RequestStream, this.Context.OutgoingResponse, this.ResponseStream);
    }
}
