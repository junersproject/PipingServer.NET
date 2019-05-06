using System.IO;
using System.ServiceModel.Web;

namespace Piping
{
    public class Req
    {
        public IncomingWebRequestContext Request;
        public Stream RequestStream;
        public OutgoingWebResponseContext Response;
        public Stream ResponseStream;
        public Req(IncomingWebRequestContext request, Stream requestStream, OutgoingWebResponseContext response, Stream responseStream)
            => (this.Request,this.RequestStream, this.Response, this.ResponseStream) = (request, requestStream, response, responseStream);
    }
}
