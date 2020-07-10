using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static DebugUtils;

namespace Piping.Server.App.APITests
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
        public async Task PutAndOneGetExample(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await PutAndOneGet(GetCreateClient(provider), Token: Source.Token);
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
        public async Task PostAndOneGetTextMultipartExample(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.PostAndOneGetTextMultipart(GetCreateClient(provider), Token: Source.Token);
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
        public async Task PostAndOneGetFileMultipartExample(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.PostAndOneGetFileMultipart(GetCreateClient(provider), Token: Source.Token);
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
        public async Task GetVersionExample(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.GetVersion(GetCreateClient(provider), Token: Source.Token);
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
        public async Task GetRootExample(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.GetRoot(GetCreateClient(provider), Token: Source.Token);
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
        public async Task GetRootExample2(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.GetRoot2(GetCreateClient(provider), Token: Source.Token);
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
        public async Task GetHelpExample(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.GetHelp(GetCreateClient(provider), Token: Source.Token);
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
        public async Task OptionsRootExample(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.OptionsRoot(GetCreateClient(provider), Token: Source.Token);
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
        public async Task PostRootExample(Uri pipingServerUri)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUri);
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.PostRoot(GetCreateClient(provider), Token: Source.Token);
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
