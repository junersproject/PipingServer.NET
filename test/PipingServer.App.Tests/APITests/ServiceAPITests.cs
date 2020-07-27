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
using static DebugUtils;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace PipingServer.App.APITests
{
    [TestClass]
    public class ServiceAPITests : TestBase
    {
        IServiceProvider Provider = null!;
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
            var services = new ServiceCollection();
            services
                .AddSingleton(server);
            services
                .AddHttpClient(Options.DefaultName, (Provider,HttpClient) => HttpClient.BaseAddress = Provider.GetRequiredService<TestServer>().BaseAddress)
                .ConfigurePrimaryHttpMessageHandler(Provider => Provider.GetRequiredService<TestServer>().CreateHandler());
            Provider = services.BuildServiceProvider();
        }
        protected IPipingServerClient CreateClient() => new PipingServerClient(Provider.GetRequiredService<IHttpClientFactory>(), Options.Create(new PipingServerClientOptions()));
        [TestCleanup]
        public void Cleanup()
        {
            if(Provider is IDisposable Disposable)
                Disposable.Dispose();
        }
        [TestMethod, TestCategory("Example")]
        public async Task PutAndOneGetAsync()
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PutAndOneGetAsync(CreateClient(), Token: Source.Token);
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
                await _PostAndOneGetTextMultipartAsync(CreateClient(), Token: Source.Token);
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
                await _PostAndOneGetFileMultipartAsync(CreateClient(), Token: Source.Token);
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
                await _GetVersionAsync(CreateClient(), Token: Source.Token);
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
                await _GetRootAsync(CreateClient(), Token: Source.Token);
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
                await GetHelpAsync(CreateClient(), Token: Source.Token);
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
                await _OptionsRootAsync(CreateClient(), Token: Source.Token);
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
                await _PostRootAsync(CreateClient(), Token: Source.Token);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
    }
}
