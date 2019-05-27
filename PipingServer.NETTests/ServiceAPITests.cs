using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.ServiceModel;
using System.Threading.Tasks;
using static DebugUtils;

namespace Piping.Tests
{
    [TestClass()]
    public class ServiceAPITests : RequestTestBase
    {
        [TestMethod, TestCategory("ShortTime")]
        public void InstanceTest()
        {
            var Uri = new Uri("http://localhost/InstanceTest");
            using var Host = new SelfHost();
            try { 
                Host.Open(Uri);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task PutAndOneGetTest()
        {
            using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
            var BaseUri = new Uri("http://localhost/" + nameof(PutAndOneGetTest));
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var SendUri = new Uri(BaseUri, "./" + nameof(PutAndOneGetTest) + "/" + nameof(PutAndOneGetTest));
                var message = "Hello World.";
                using var HostDispose = Source.Token.Register(() => Host.Dispose());
                Trace.WriteLine($"BASE URL: {BaseUri}");
                Trace.WriteLine($"TARGET URL: {SendUri}");
                var (_, Version) = await GetVersionAsync(BaseUri);
                Trace.WriteLine($"VERSION: {Version}");
                await PipingServerPutAndGetMessageSimple(SendUri, message, Source.Token);
            }catch(AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }

        [TestMethod, TestCategory("ShortTime")]
        public async Task GetVersionTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetVersionTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetVersionTest) + "/version");
            using var Host = new SelfHost();
            try { 
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            } catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetTopPageTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetTopPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetTopPageTest) + "/");
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            } catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetTopPageTest2()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetTopPageTest));
            var SendUri = BaseUri;
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetHelpPageTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetHelpPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetHelpPageTest) + "/help");
            using var Host = new SelfHost();
            try
            {
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
            catch (AddressAccessDeniedException e)
            {
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
    }
}
