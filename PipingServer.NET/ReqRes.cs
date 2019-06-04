using System.IO;
using System.ServiceModel.Web;
namespace Piping
{
    public class ReqRes
    {
        public WebOperationContext Context { get; set; }
        public Stream? RequestStream = null;
        public Stream? ResponseStream = null;

        public ReqRes(WebOperationContext Context) => this.Context = Context;
    }
}
