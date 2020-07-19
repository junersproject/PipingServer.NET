using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipingServer.Client;
using static PipingServer.App.APITests.HttpClientFactory;
using static DebugUtils;

namespace PipingServer.App.APITests
{
    [TestClass]
    public class ServiceAPITests : TestBase
    {
        IDisposable? disposable;
        IHttpClientFactory HttpClientFactory = null!;
        [TestInitialize]
        public void Initialize()
        {
            var builder = WebHost.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    if (!Debugger.IsAttached)
                        logging.ClearProviders();
                })
                .UseStartup<Startup>();
            var server = new TestServer(builder);
            var list = DisposableList.Create(server);
            HttpClientFactory = Create(server);
            disposable = list;
        }
        [TestCleanup]
        public void Cleanup()
        {
            disposable!.Dispose();
        }
        [TestMethod, TestCategory("Example")]
        public async Task PutAndOneGetAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PutAndOneGetAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task PostAndOneGetTextMultipartAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PostAndOneGetTextMultipartAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task PostAndOneGetFileMultipartAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PostAndOneGetFileMultipartAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task GetVersionAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _GetVersionAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example")]
        [Description("piping-server の / を取得する")]
        public async Task GetRootAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _GetRootAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        [Description("piping-server の ルート を取得する")]
        public async Task GetRoot2Async()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _GetRoot2Async(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example")]
        [Description("piping-server の /help の取得を試みる。")]
        public async Task GetHelpAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await GetHelpAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task OptionsRootAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _OptionsRootAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task PostRootAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PostRootAsync(HttpClientFactory, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
    }
}
