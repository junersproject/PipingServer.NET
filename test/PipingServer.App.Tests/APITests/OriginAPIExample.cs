using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DebugUtils;

namespace PipingServer.App.APITests
{
    [TestClass]
    public class OriginAPIExample : TestBase
    {
        static IEnumerable<object[]> OriginPipingServerUrls
        {
            get
            {
                yield return new object[] { new Uri("https://ppng.ml") };
                yield return new object[] { new Uri("https://piping-92sr2pvuwg14.runkit.sh") };
            }
        }
        private ServiceProvider CreateProvider(Uri pipingServerUrl)
        {
            var services = new ServiceCollection();
            services.AddHttpClient("piping-server", c =>
            {
                c.BaseAddress = pipingServerUrl;
            });
            return services.BuildServiceProvider();
        }
        private Func<HttpClient> GetCreateClient(IServiceProvider provider)
        {
            var factory = provider.GetRequiredService<IHttpClientFactory>();
            return () => factory.CreateClient("piping-server");
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PutAndOneGetExampleAsync(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PutAndOneGetAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PostAndOneGetTextMultipartExampleAsync(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._PostAndOneGetTextMultipartAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PostAndOneGetFileMultipartExampleAsync(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._PostAndOneGetFileMultipartAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task GetVersionExampleAsync(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._GetVersionAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        [Description("piping-server の / を取得する")]
        public async Task GetRootExampleAsync(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._GetRootAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        [Description("piping-server の ルート を取得する")]
        public async Task GetRootExample2Async(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._GetRoot2Async(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        [Description("piping-server の /help の取得を試みる。")]
        public async Task GetHelpExampleAsync(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.GetHelpAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task OptionsRootExampleAsync(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._OptionsRootAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PostRootExampleAsync(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._PostRootAsync(GetCreateClient(provider), Token: Source.Token);
            }
            catch (AssertFailedException e)
            {
                throw new AssertInconclusiveException("サポートされていないバージョン？", e);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                ThrowIfHostIsUnknown(e);
                throw;
            }
        }
    }
}
