using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Piping.Tests
{
    [TestClass()]
    public class ServiceAPITests
    {
        [TestMethod, TestCategory("ShortTime")]
        public void InstanceTest()
        {
            var Uri = new Uri("http://localhost/InstanceTest");
            using (var Host = new SelfHost())
            {
                Host.Open(Uri);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public void UploadTest()
        {
            using (var Host = new SelfHost())
            {
                var BaseUri = new Uri("http://localhost/" + nameof(UploadTest));
                var SendUri = new Uri(BaseUri, "./" + nameof(UploadTest) + "/UploadTest");
                Host.Open(BaseUri);
                var message = "Hello World.";
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(message)))
                {
                    var request = WebRequest.Create(SendUri) as HttpWebRequest;
                    request.Method = "PUT";
                    request.ContentType = "application/octet-stream";
                    request.ContentLength = stream.Length;
                    request.AllowWriteStreamBuffering = true;
                    request.AllowReadStreamBuffering = false;
                    // タイムアウト6h
                    request.Timeout = 360 * 60 * 1000;
                    request.ReadWriteTimeout = 360 * 60 * 1000;
                    try
                    {
                        using (Stream requestStream = request.GetRequestStream())
                        {
                            stream.CopyTo(requestStream);
                        }
                    }
                    catch (Exception)
                    {
                        // nop
                    }
                    request.GetResponse();
                }
            }
        }

        [TestMethod, TestCategory("ShortTime")]
        public async Task GetVersionTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetVersionTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetVersionTest) + "/version");
            using (var Host = new SelfHost())
            {
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetTopPageTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetTopPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetTopPageTest) + "/");
            using (var Host = new SelfHost())
            {
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetTopPageTest2()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetTopPageTest));
            var SendUri = BaseUri;
            using (var Host = new SelfHost())
            {
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetHelpPageTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetHelpPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetHelpPageTest) + "/help");
            using (var Host = new SelfHost())
            {
                Host.Open(BaseUri);
                var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
                Trace.WriteLine(Headers);
                Trace.WriteLine(BodyText);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <param name="SendUri"></param>
        /// <param name="Method"></param>
        /// <returns></returns>
        internal async Task<(WebHeaderCollection Headers, string BodyText)> GetResponseAsync(Uri SendUri, string Method)
        {
            var request = WebRequest.Create(SendUri) as HttpWebRequest;
            request.Method = Method;
            request.AllowWriteStreamBuffering = true;
            request.AllowReadStreamBuffering = false;
            // タイムアウト6h
            request.Timeout = 360 * 60 * 1000;
            request.ReadWriteTimeout = 360 * 60 * 1000;
            var response = request.GetResponse();
            var resStream = response.GetResponseStream();
            using (var reader = new StreamReader(resStream, Encoding.UTF8, false))
                return (response.Headers, await reader.ReadToEndAsync());
        }
    }
}
