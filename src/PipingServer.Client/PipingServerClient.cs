using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Threading;
using System.IO;
using System;

namespace PipingServer.Client
{
    public class PipingServerClient
    {
        readonly IHttpClientFactory HttpClientFactory;
        public PipingServerClient(IHttpClientFactory HttpClientFactory)
            => this.HttpClientFactory = HttpClientFactory;
        public async Task<string> GetVersionAsync(CancellationToken Token = default)
        {
            var (_, _, _, text) = await GetResponseAsync("/version", HttpMethod.Get, Token);
            return text;
        }
        public async Task<string> GetHelpAsync(CancellationToken Token = default)
        {
            var (_, _, _, text) = await GetResponseAsync("/help", HttpMethod.Get, Token);
            return text;
        }
        public async Task<string> GetTopPageAsync(CancellationToken Token = default)
        {
            var (_, _, _, text) = await GetResponseAsync("", HttpMethod.Get, Token);
            return text;
        }
        public Task<HttpResponseMessageResult> UploadAsync(string requestKey, HttpContent Content, CancellationToken Token = default)
            => UploadAsync(requestKey, Content, null, Token);
        public Task<HttpResponseMessageResult> UploadAsync(string requestKey, HttpContent Content, Action<HttpRequestHeaders>? HeaderSetAction = null, CancellationToken Token = default)
            => RequestAsync(requestKey, HttpMethod.Put, Content, HeaderSetAction, Token);
        public Task<HttpResponseMessageResult> DownloadAsync(string requestKey, HttpContent Content, Action<HttpRequestHeaders>? HeaderSetAction = null, CancellationToken Token = default)
            => RequestAsync(requestKey, HttpMethod.Get, Content, HeaderSetAction, Token);
        public async Task<HttpResponseMessageResult> RequestAsync(string requestKey, HttpMethod HttpMethod, HttpContent Content, Action<HttpRequestHeaders>? HeaderSetAction = null, CancellationToken Token = default)
        {
            var Disposable = DisposableList.Create();
            var client = HttpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod, requestKey);
            request.Content = Content;
            Disposable.Add(request);
            HeaderSetAction?.Invoke(request.Headers);
            using var response = (await client.SendAsync(request, Token)).EnsureSuccessStatusCode();
            return new HttpResponseMessageResult(response, Disposable);

        }
        async Task<(HttpStatusCode StatusCode, HttpResponseHeaders Headers, HttpContentHeaders Cheaders, string BodyText)> GetResponseAsync(string SendUri, HttpMethod Method, CancellationToken Token = default)
        {
            var client = HttpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(Method, SendUri);
            using var response = (await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, Token)).EnsureSuccessStatusCode();
            using var resStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(resStream, Encoding.UTF8, true);
            return (response.StatusCode, response.Headers, response.Content.Headers, await reader.ReadToEndAsync());
        }
    }
}
