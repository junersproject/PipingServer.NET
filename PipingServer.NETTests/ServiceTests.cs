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
    public class ServiceTests
    {
        [TestMethod,TestCategory("ShortTime")]
        public void InstanceTest()
        {
            var Uri = new Uri("http://localhost:8080/InstanceTest");
            using (var Host = new SelfHost())
            {
                Host.Open(Uri);
            }
        }
        [TestMethod,TestCategory("ShortTime")]
        public void UploadTest()
        {
            using (var Host = new SelfHost())
            {
                var BaseUri = new Uri("http://localhost:8080/UploadTest");
                var SendUri = new Uri(BaseUri, "UploadTest");
                Host.Open(BaseUri);
                var message = "Hello World.";
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(message)))
                {
                    HttpWebRequest request = WebRequest.Create(SendUri) as HttpWebRequest;
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
            using (var Host = new SelfHost())
            {
                var BaseUri = new Uri("http://localhost:8080/version");
                var SendUri = new Uri(BaseUri, "version");
                Host.Open(BaseUri);
                HttpWebRequest request = WebRequest.Create(SendUri) as HttpWebRequest;
                request.Method = "GET";
                request.AllowWriteStreamBuffering = true;
                request.AllowReadStreamBuffering = false;
                // タイムアウト6h
                request.Timeout = 360 * 60 * 1000;
                request.ReadWriteTimeout = 360 * 60 * 1000;
                var response = request.GetResponse();
                var resStream = response.GetResponseStream();
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8, false))
                {
                    Trace.WriteLine(await reader.ReadToEndAsync());
                }
            }
        }
    }
}