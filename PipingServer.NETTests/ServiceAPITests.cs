using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading;
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
        public async Task PutAndOneGetTest()
        {
            using var Source = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var BaseUri = new Uri("http://localhost/" + nameof(PutAndOneGetTest));
            using var Host = new SelfHost();
            Host.Open(BaseUri);
            var SendUri = new Uri(BaseUri, "./" + nameof(PutAndOneGetTest) + "/"+nameof(PutAndOneGetTest));
            var message = "Hello World.";
            using var HostDispose = Source.Token.Register(() => Host.Dispose());
            await PipingServerPutAndGetMessageSimple(SendUri, message, Source.Token);
        }
        [TestMethod, TestCategory("Example")]
        public async Task PutAndOneGetOriginPipingServer()
        {
            using var Source = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var SendUri = new Uri("https://ppng.ml/" + nameof(PutAndOneGetOriginPipingServer));
            var message = "Hello World.";
            await PipingServerPutAndGetMessageSimple(SendUri, message, Source.Token);
        }
        protected async Task PipingServerPutAndGetMessageSimple(Uri SendUri, string message, CancellationToken Token)
        {
            var sender = Task.Run(async () =>
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));
                var request = WebRequest.Create(SendUri) as HttpWebRequest;
                request.Method = "POST";
                request.ContentType = "text/plain;charset=UTF-8";
                request.ContentLength = stream.Length;
                request.AllowWriteStreamBuffering = true;
                // タイムアウト6h
                request.Timeout = 360 * 60 * 1000;
                request.ReadWriteTimeout = 360 * 60 * 1000;
                foreach (var (Key, Value) in request.Headers.AllKeys.Select(k => (k, request.Headers.Get(k))))
                    Trace.WriteLine($"[SEND HEADER] : {Key} : {Value}");
                Trace.WriteLine("[SENDER REQUEST] [START]");
                try
                {
                    {
                        using var requestStream = await request.GetRequestStreamAsync();
                        using var requestStreamDispose = Token.Register(() => requestStream.Dispose());
                        await stream.CopyToAsync(requestStream, 1024, Token);
                        Trace.WriteLine("[SENT MESSAGE] : " + message);
                    }
                    foreach (var (Key, Value) in request.Headers.AllKeys.Select(k => (k, request.Headers.Get(k))))
                        Trace.WriteLine($"[SENT HEADER] : {Key} : {Value}");
                }
                finally
                {
                    Trace.WriteLine("[SENDER REQUEST] [END]");
                }
                Trace.WriteLine("[SENDER RESPONSE] [START]");
                try
                {
                    using var response = await request.GetResponseAsync();
                    using var responseDispose = Token.Register(() => response.Dispose());
                    foreach (var (Key, Value) in response.Headers.AllKeys.Select(k => (k, response.Headers.Get(k))))
                        Trace.WriteLine($"[SENDER'S RESPONSE HEADER] : {Key} : {Value}");
                    using var outstream = response.GetResponseStream();
                    using var outstreamDispose = Token.Register(() => outstream.Dispose());
                    using var reader = new StreamReader(outstream, Encoding.UTF8, false, 1024, true);
                    string Line;
                    string ReadToEnd = string.Empty;
                    while (!string.IsNullOrEmpty(Line = await reader.ReadLineAsync()))
                    {
                        Trace.WriteLine("[SENDER RESPONSE] : " + Line);
                        ReadToEnd += Line;
                    }
                    return ReadToEnd;
                }
                finally
                {
                    Trace.WriteLine("[SENDER RESPONSE] [END]");
                }
            });
            var receiver = Task.Run(async () =>
            {
                var request = WebRequest.Create(SendUri) as HttpWebRequest;
                request.Method = "GET";
                request.AllowWriteStreamBuffering = false;
                // タイムアウト6h
                request.Timeout = 360 * 60 * 1000;
                request.ReadWriteTimeout = 360 * 60 * 1000;

                Trace.WriteLine("[RECEIVER RESPONSE] [START]");
                try
                {
                    using var response = await request.GetResponseAsync();
                    using var responseDispose = Token.Register(() => response.Dispose());
                    foreach (var (Key, Value) in response.Headers.AllKeys.Select(k => (k, response.Headers.Get(k))))
                        Trace.WriteLine($"[RESPONSE HEADER] : {Key} : {Value}");
                    using var stream = response.GetResponseStream();
                    using var streamDispose = Token.Register(() => stream.Dispose());
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                    string Line;
                    string ReadToEnd = string.Empty;
                    while (!string.IsNullOrEmpty(Line = await reader.ReadLineAsync()))
                    {
                        Trace.WriteLine("[RECEIVE MESSAGE] : " + Line);
                        ReadToEnd += Line;
                    }
                    return ReadToEnd;
                }
                catch (WebException e)
                {
                    Trace.WriteLine(e.Status);
                    throw;
                }
                finally
                {
                    Trace.WriteLine("[RECEIVER RESPONSE] [END]");
                }
            });
            await Task.WhenAll(sender, receiver);
            Assert.AreEqual(message, await receiver);
        }

        [TestMethod, TestCategory("ShortTime")]
        public async Task GetVersionTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetVersionTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetVersionTest) + "/version");
            using var Host = new SelfHost();
            Host.Open(BaseUri);
            var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
            Trace.WriteLine(Headers);
            Trace.WriteLine(BodyText);
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetTopPageTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetTopPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetTopPageTest) + "/");
            using var Host = new SelfHost();
            Host.Open(BaseUri);
            var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
            Trace.WriteLine(Headers);
            Trace.WriteLine(BodyText);
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetTopPageTest2()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetTopPageTest));
            var SendUri = BaseUri;
            using var Host = new SelfHost();
            Host.Open(BaseUri);
            var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
            Trace.WriteLine(Headers);
            Trace.WriteLine(BodyText);
        }
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetHelpPageTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetHelpPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetHelpPageTest) + "/help");
            using var Host = new SelfHost();
            Host.Open(BaseUri);
            var (Headers, BodyText) = await GetResponseAsync(SendUri, "GET");
            Trace.WriteLine(Headers);
            Trace.WriteLine(BodyText);
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
            using var response = await request.GetResponseAsync();
            using var resStream = response.GetResponseStream();
            using var reader = new StreamReader(resStream, Encoding.UTF8, true);
            return (response.Headers, await reader.ReadToEndAsync());
        }
    }
}
