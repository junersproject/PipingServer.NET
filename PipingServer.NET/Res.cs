using System;
using System.IO;
using System.ServiceModel.Web;

namespace Piping
{
    public class Res
    {
        public IncomingWebRequestContext Request;
        public Stream RequestStream;
        public OutgoingWebResponseContext Response;
        public Action<Stream> SetResponseStream;
        public Res(IncomingWebRequestContext request, Stream requestStream, OutgoingWebResponseContext response, Action<Stream> setResponseStream)
            => (this.Request, this.RequestStream, this.Response, this.SetResponseStream) = (request, requestStream, response, setResponseStream);
    }
}
