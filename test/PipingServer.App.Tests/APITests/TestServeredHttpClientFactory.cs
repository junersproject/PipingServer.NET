using System;
using System.Net.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Options;

namespace PipingServer.App.APITests
{
    internal static class HttpClientFactory
    {
        public static IHttpClientFactory Create(Uri BaseUri) => throw new NotImplementedException();
        internal class UrlHttpClientFactory : IHttpClientFactory
        {
            readonly HttpClient HttpClient;
            public UrlHttpClientFactory(Uri BaseUri)
            {
                HttpClient = new HttpClient
                {
                    BaseAddress = BaseUri,
                };
            }
            public HttpClient CreateClient(string name)
            {
                if (Options.DefaultName != name)
                    throw new NotImplementedException();
                return HttpClient;
            }
        }

        public static IHttpClientFactory Create(TestServer TestServer) => new TestServeredHttpClientFactory(TestServer);
        internal class TestServeredHttpClientFactory : IHttpClientFactory
        {
            readonly TestServer TestServer;
            public TestServeredHttpClientFactory(TestServer TestServer)
                => this.TestServer = TestServer;
            public HttpClient CreateClient(string name)
            {
                if (Options.DefaultName != name)
                    throw new NotImplementedException();
                return TestServer.CreateClient();
            }
        }
    }
}
