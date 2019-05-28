using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using static DebugUtils;

namespace Piping.Tests
{
    [TestClass()]
    public class ServiceAPITests : RequestTestBase
    {
        static IEnumerable<object[]> LocalPipingServerUrls
        {
            get
            {
                yield return new object[] { "http://localhost", };
                //yield return new object[] { "https://localhost" };
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public void InstanceTest(string localPipingServerUrl)
        {
            var Uri = new Uri( localPipingServerUrl.TrimEnd('/') + "/InstanceTest");
            using var Host = new SelfHost();
            try { 
                Host.Open(Uri);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public async Task PutAndOneGetTest(string localPipingServerUrl)
        {
            using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
            var BaseUri = new Uri(localPipingServerUrl.TrimEnd('/') + "/" + nameof(PutAndOneGetTest));
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var SendUri = new Uri(BaseUri, "./" + nameof(PutAndOneGetTest) + "/" + nameof(PutAndOneGetTest));
                var message = "Hello World.";
                using var HostDispose = Source.Token.Register(() => Host.Dispose());
                Trace.WriteLine($"BASE URL: {BaseUri}");
                Trace.WriteLine($"TARGET URL: {SendUri}");
                var (_, _, Version) = await GetVersionAsync(BaseUri);
                Trace.WriteLine($"VERSION: {Version}");
                await PipingServerPutAndGetMessageSimple(SendUri, message, Source.Token);
            }catch(AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }

        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public async Task GetVersionTest(string localPipingServerUrl)
        {
            var BaseUri = new Uri(localPipingServerUrl.TrimEnd('/') + "/" + nameof(GetVersionTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetVersionTest) + "/version");
            using var Host = new SelfHost();
            try { 
                Host.Open(BaseUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            } catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public async Task GetTopPageTest(string localPipingServerUrl)
        {
            var BaseUri = new Uri(localPipingServerUrl.TrimEnd('/') + "/" + nameof(GetTopPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetTopPageTest) + "/");
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
                //CollectionAssert.Contains((Headers.TryGetValues("Content-Type", out var Value) ? Value : Enumerable.Empty<string>()).ToArray(), "text/html", "Content-Type");
            } catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public async Task GetTopPageTest2(string localPipingServerUrl)
        {
            var BaseUri = new Uri(localPipingServerUrl.TrimEnd('/') + "/" + nameof(GetTopPageTest));
            var SendUri = BaseUri;
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public async Task GetHelpPageTest(string localPipingServerUrl)
        {
            var BaseUri = new Uri(localPipingServerUrl.TrimEnd('/') + "/" + nameof(GetHelpPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetHelpPageTest) + "/help");
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public async Task OptionsRootTest(string localPipingServerUrl)
        {

            var BaseUri = new Uri(localPipingServerUrl.TrimEnd('/') + "/" + nameof(OptionsRootTest));
            var SendUri = BaseUri;
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Options);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime"), DynamicData(nameof(LocalPipingServerUrls))]
        public async Task PostRootTest(string localPipingServerUrl)
        {

            var BaseUri = new Uri(localPipingServerUrl.TrimEnd('/') + "/" + nameof(PostRootTest));
            var SendUri = BaseUri;
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Status, Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Post);
                Trace.WriteLine(Status);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
                Assert.AreEqual(HttpStatusCode.BadRequest, Status);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
    }
}
