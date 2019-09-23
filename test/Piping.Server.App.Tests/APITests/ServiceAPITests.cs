using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Piping.Server.App.Tests;
using static DebugUtils;

namespace Piping.Server.App.APITests
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
        public async Task PutAndOneGet()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await PutAndOneGet(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task PostAndOneGetTextMultipart()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await PostAndOneGetTextMultipart(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task PostAndOneGetFileMultipart()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await PostAndOneGetFileMultipart(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task GetVersion()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await GetVersion(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example")]
        [Description("piping-server の / を取得する")]
        public async Task GetRoot()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await GetRoot(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        [Description("piping-server の ルート を取得する")]
        public async Task GetRoot2()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await GetRoot2(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example")]
        [Description("piping-server の /help の取得を試みる。")]
        public async Task GetHelp()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await GetHelp(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task OptionsRoot()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await OptionsRoot(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example")]
        public async Task PostRoot()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await PostRoot(GetCreateClient!, Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
    }
}
