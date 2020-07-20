using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipingServer.Client;
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
                yield return OriginPipingServerUrls(new Uri("https://ppng.ml"));
                yield return OriginPipingServerUrls(new Uri("https://piping-92sr2pvuwg14.runkit.sh"));
                static object[] OriginPipingServerUrls(Uri Uri) => new object[] { Uri, };
            }
        }
        private ServiceProvider CreateProvider(Uri pipingServerUrl)
        {
            var services = new ServiceCollection();
            services.AddHttpClient(Options.DefaultName, c =>
            {
                c.BaseAddress = pipingServerUrl;
            });
            services.AddTransient<IPipingServerClient, PipingServerClient>();
            return services.BuildServiceProvider();
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PutAndOneGetExampleAsync(Uri pipingServerUrl)
        {
            try
            {
                using var provider = CreateProvider(pipingServerUrl);
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PutAndOneGetAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._PostAndOneGetTextMultipartAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._PostAndOneGetFileMultipartAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._GetVersionAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._GetRootAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base.GetHelpAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await base._OptionsRootAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
                var Client = provider.GetRequiredService<IPipingServerClient>();
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                await _PostRootAsync(Client, Token: Source.Token);
            }
            catch (SimpleHttpResponseException e)
            {
                Trace.WriteLine(e);
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
