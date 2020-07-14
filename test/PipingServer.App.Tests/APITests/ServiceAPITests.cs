using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipingServer.App.Tests;
using static DebugUtils;

namespace PipingServer.App.APITests
{
    [TestClass]
    public class ServiceAPITests : TestBase
    {
        IDisposable? disposable;
        Func<HttpClient>? GetCreateClient;
        [TestInitialize]
        public void Initialize()
        {
            var builder = WebHost.CreateDefaultBuilder()
                .ConfigureLogging(logging => logging.ClearProviders())
                .UseStartup<Startup>();
            var server = new TestServer(builder);
            var list = new DisposableList
            {
                server
            };
            GetCreateClient = server.CreateClient;
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
                await _PutAndOneGetAsync(GetCreateClient!, Token: Source.Token);
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
                await _PostAndOneGetTextMultipartAsync(GetCreateClient!, Token: Source.Token);
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
                await _PostAndOneGetFileMultipartAsync(GetCreateClient!, Token: Source.Token);
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
                await _GetVersionAsync(GetCreateClient!, Token: Source.Token);
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
                await _GetRootAsync(GetCreateClient!, Token: Source.Token);
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
                await _GetRoot2Async(GetCreateClient!, Token: Source.Token);
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
                await GetHelpAsync(GetCreateClient!, Token: Source.Token);
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
                await _OptionsRootAsync(GetCreateClient!, Token: Source.Token);
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
                await _PostRootAsync(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
    }
}
