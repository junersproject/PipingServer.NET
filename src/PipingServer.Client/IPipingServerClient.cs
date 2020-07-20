using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace PipingServer.Client
{
    public interface IPipingServerClient
    {
        protected abstract IHttpClientFactory HttpClientFactory { get; }
        protected abstract PipingServerClientOptions Options { get; }
        public HttpClient CreateClient() => HttpClientFactory.CreateClient(Options.DefaultName);
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage Message, HttpCompletionOption CompletionOption, CancellationToken Token) => CreateClient().SendAsync(Message, CompletionOption, Token);
        public async Task<HttpResponseMessageResult> RequestAsync(string requestKey, HttpMethod HttpMethod, HttpContent? Content = null, HttpRequestHeaders? Headers = null, CancellationToken Token = default)
        {
            var Disposable = DisposableList.Create();
            using var request = new HttpRequestMessage(HttpMethod, requestKey);
            if (Content is HttpContent)
                request.Content = Content;
            Disposable.Add(request);
            if (Headers is HttpRequestHeaders)
                foreach (var header in Headers)
                {
                    if (request.Headers.Contains(header.Key))
                        request.Headers.Remove(header.Key);
                    request.Headers.Add(header.Key, header.Value);
                }
            var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
            Disposable.Add(response);
            return new HttpResponseMessageResult(response, Disposable);
        }
        public async Task<string> GetTextAsync(string SendUri, HttpMethod Method, CancellationToken Token)
        {
            using var response = await RequestAsync(SendUri, Method, null, null, Token);
            var Content = await response.Message.Content.ReadAsStringAsync();
            if (response.Message.IsSuccessStatusCode)
                return Content;
            throw new SimpleHttpResponseException(response.Message.StatusCode, Content);
        }

        public async Task<string> GetVersionAsync(CancellationToken Token = default)
            => await GetTextAsync("/version", HttpMethod.Get, Token);
        public async Task<string> GetHelpAsync(CancellationToken Token = default)
            => await GetTextAsync("/help", HttpMethod.Get, Token);
        public async Task<string> GetRootPageAsync(CancellationToken Token = default)
            => await GetTextAsync("", HttpMethod.Get, Token);
        public Task<HttpResponseHeaders> GetOptionsAsync(CancellationToken Token = default)
            => GetOptionsAsync("", Token);
        public async Task<HttpResponseHeaders> GetOptionsAsync(string SendUri, CancellationToken Token = default)
        {
            var Method = HttpMethod.Options;
            using var response = await RequestAsync(SendUri, Method, null, null, Token);
            if (response.Message.IsSuccessStatusCode)
                return response.Message.Headers;
            var Content = await response.Message.Content.ReadAsStringAsync();
            throw new SimpleHttpResponseException(response.Message.StatusCode, Content);
        }
        public Task<HttpResponseMessageResult> UploadAsync(string requestKey, HttpContent Content, CancellationToken Token = default)
            => UploadAsync(requestKey, Content, null, Token);
        public Task<HttpResponseMessageResult> UploadAsync(string requestKey, HttpContent Content, HttpRequestHeaders? Headers = null, CancellationToken Token = default)
            => RequestAsync(requestKey, HttpMethod.Put, Content, Headers, Token);
        public Task<HttpResponseMessageResult> DownloadAsync(string requestKey, HttpContent Content, HttpRequestHeaders? Headers = null, CancellationToken Token = default)
            => RequestAsync(requestKey, HttpMethod.Get, Content, Headers, Token);
    }
}
