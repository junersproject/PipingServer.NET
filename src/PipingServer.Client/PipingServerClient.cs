using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.IO;
using Microsoft.Extensions.Options;

namespace PipingServer.Client
{
    public partial class PipingServerClient : IPipingServerClient
    {
        protected IHttpClientFactory HttpClientFactory { get; }
        IHttpClientFactory IPipingServerClient.HttpClientFactory => HttpClientFactory;
        protected PipingServerClientOptions Options { get; }
        PipingServerClientOptions IPipingServerClient.Options => Options;

        public PipingServerClient(IHttpClientFactory HttpClientFactory, IOptions<PipingServerClientOptions>? Options = null)
            => (this.HttpClientFactory, this.Options) = (HttpClientFactory, Options?.Value ?? new PipingServerClientOptions());
    }
}
