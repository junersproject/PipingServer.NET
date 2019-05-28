﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using static DebugUtils;

namespace Piping.Tests
{
    [TestClass]
    public class OriginServiceAPIExample : RequestTestBase
    {
        static IEnumerable<object[]> OriginPipingServerUrls
        {
            get
            {
                yield return new object[] { "https://ppng.ml" };
                yield return new object[] { "https://piping.arukascloud.io" };
                yield return new object[] { "https://piping-92sr2pvuwg14.runkit.sh" };
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PutAndOneGetExample(string pipingServerUrl)
        {
            try
            {
                using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
                var BaseUri = new Uri(pipingServerUrl);
                var SendUri = new Uri(BaseUri.ToString().TrimEnd('/') + "/" + nameof(PutAndOneGetExample));
                var message = "Hello World.";
                Trace.WriteLine($"BASE URL: {BaseUri}");
                Trace.WriteLine($"TARGET URL: {SendUri}");
                var (_, _, Version) = await GetVersionAsync(BaseUri);
                Trace.WriteLine($"VERSION: {Version}");
                await PipingServerPutAndGetMessageSimple(SendUri, message, Token: Source.Token);
            }catch(HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task GetVersionExample(string pipingServerUri)
        {
            try { 
                var BaseUri = new Uri(pipingServerUri);
                var SendUri = new Uri(BaseUri, "/version");
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        [Description("piping-server の / を取得する")]
        public async Task GetRootExample(string pipingServerUri)
        {
            try { 
                var BaseUri = new Uri(pipingServerUri.TrimEnd('/') + "/");
                var (Status, Headers, BodyText) = await GetResponseAsync(BaseUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
                Assert.AreEqual(HttpStatusCode.OK, Status);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        [Description("piping-server の ルート を取得する")]
        public async Task GetRootExample2(string pipingServerUri)
        {
            try
            {
                var BaseUri = new Uri(pipingServerUri.TrimEnd('/'));
                var (Status, Headers, BodyText) = await GetResponseAsync(BaseUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
                Assert.AreEqual(HttpStatusCode.OK, Status);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }

        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        [Description("piping-server の /help の取得を試みる。")]
        public async Task GetHelpExample(string pipingServerUri)
        {
            try { 
                var BaseUri = new Uri(pipingServerUri);
                var SendUri = new Uri(BaseUri, "/help");
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
                Assert.AreEqual(HttpStatusCode.OK, Status);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task OptionsRootExample(string pipingServerUri)
        {
            try { 
                var BaseUri = new Uri(pipingServerUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(BaseUri, HttpMethod.Options);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
                Assert.AreEqual(HttpStatusCode.OK, Status);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        [TestMethod, TestCategory("Example"), DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PostRootExample(string pipingServerUri)
        {
            try { 
                var BaseUri = new Uri(pipingServerUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(BaseUri, HttpMethod.Post);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
                Assert.AreEqual(HttpStatusCode.BadRequest, Status);
            }
            catch (HttpRequestException e)
            {
                ThrowIfCoundNotResolveRemoteName(e);
                throw;
            }
        }
        private void ThrowIfCoundNotResolveRemoteName(HttpRequestException e)
        {
            if (e.HResult == -2146233088)
            {
                Trace.WriteLine(e);
                throw new AssertInconclusiveException("リモート名の解決に失敗", e);
            }
        }
    }
}
