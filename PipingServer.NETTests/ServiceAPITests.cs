using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DebugUtils;

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
            using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
            var BaseUri = new Uri("http://localhost/" + nameof(PutAndOneGetTest));
            using var Host = new SelfHost();
            Host.Open(BaseUri);
            var SendUri = new Uri(BaseUri, "./" + nameof(PutAndOneGetTest) + "/"+nameof(PutAndOneGetTest));
            var message = "Hello World.";
            using var HostDispose = Source.Token.Register(() => Host.Dispose());
            Trace.WriteLine($"BASE URL: {BaseUri}");
            Trace.WriteLine($"TARGET URL: {SendUri}");
            var (_, Version) = await GetVersionAsync(BaseUri);
            Trace.WriteLine($"VERSION: {Version}");
            await PipingServerPutAndGetMessageSimple(SendUri, message, Source.Token);
        }
        static IEnumerable<object[]> OriginPipingServerUrls
        {
            get
            {
                yield return new object[] { "https://ppng.ml/" };
                yield return new object[] { "https://piping.arukascloud.io/" };
                yield return new object[] { "https://piping-92sr2pvuwg14.runkit.sh/" };

            }
        }
        [TestMethod, TestCategory("Example"),DynamicData(nameof(OriginPipingServerUrls))]
        public async Task PutAndOneGetOriginPipingServerTest(string pipingServerUrl)
        {
            using var Source = CreateTokenSource(TimeSpan.FromSeconds(30));
            var BaseUri = new Uri(pipingServerUrl);
            var SendUri = new Uri(BaseUri.ToString().TrimEnd('/') + "/" + nameof(PutAndOneGetOriginPipingServerTest));
            var message = "Hello World.";
            Trace.WriteLine($"BASE URL: {BaseUri}");
            Trace.WriteLine($"TARGET URL: {SendUri}");
            var (_, Version) = await GetVersionAsync(BaseUri);
            Trace.WriteLine($"VERSION: {Version}");
            await PipingServerPutAndGetMessageSimple(SendUri, message, Token: Source.Token);
        }
        protected async Task PipingServerPutAndGetMessageSimple(Uri SendUri, string message, CancellationToken Token = default)
        {
            var sender = Task.Run(async () =>
            {
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Put, SendUri)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes(message)),
                };
                request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("UTF-8"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                foreach (var (Key, Value) in request.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                    Trace.WriteLine($"[SEND HEADER] : {Key} : [{string.Join(", ", Value)}]");
                HttpResponseMessage response;
                Trace.WriteLine("[SENDER REQUEST] [START]");
                try
                {
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
                    {
                        Trace.WriteLine("[SENT MESSAGE] : " + message);
                    }
                    foreach (var (Key, Value) in request.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENT HEADER] : {Key} : [{string.Join(", ",Value)}]");
                }
                finally
                {
                    Trace.WriteLine("[SENDER REQUEST] [END]");
                }
                Trace.WriteLine("[SENDER RESPONSE] [START]");
                try
                {
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENDER'S RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var outstream = await response.Content.ReadAsStreamAsync();
                    using var outstreamDispose = Token.Register(() => outstream.Dispose());
                    using var reader = new StreamReader(outstream, Encoding.UTF8, false, 1024, true);
                    string Line;
                    string ReadToEnd = string.Empty;
                    while (!string.IsNullOrEmpty(Line = await reader.ReadLineAsync()))
                    {
                        Trace.WriteLine($"[SENDER RESPONSE MESSAGE] : {Line}");
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
                using var client = new HttpClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, SendUri);


                Trace.WriteLine("[RECEIVER RESPONSE] [START]");
                try
                {
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
                    using var responseDispose = Token.Register(() => response.Dispose());
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var streamDispose = Token.Register(() => stream.Dispose());
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                    string Line;
                    string ReadToEnd = string.Empty;
                    while (!string.IsNullOrEmpty(Line = await reader.ReadLineAsync()))
                    {
                        Trace.WriteLine($"[RECEIVE MESSAGE] : {Line}");
                        ReadToEnd += Line;
                    }
                    return ReadToEnd;
                }
                catch (WebException e)
                {
                    Trace.WriteLine($"[RECEIVER ERROR STATUS] : {e.Status}");
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
            var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
            Trace.WriteLine(Headers);
            Trace.WriteLine(BodyText);
        }
        public async Task<(HttpResponseHeaders, string BodyText)> GetVersionAsync(Uri BaseUri) => await GetResponseAsync(new Uri(BaseUri.ToString().TrimEnd('/') + "/version"), HttpMethod.Get);
        [TestMethod, TestCategory("ShortTime")]
        public async Task GetTopPageTest()
        {
            var BaseUri = new Uri("http://localhost/" + nameof(GetTopPageTest));
            var SendUri = new Uri(BaseUri, "./" + nameof(GetTopPageTest) + "/");
            using var Host = new SelfHost();
            Host.Open(BaseUri);
            var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
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
            var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
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
            var (Headers, BodyText) = await GetResponseAsync(SendUri, HttpMethod.Get);
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
        internal async Task<(HttpResponseHeaders Headers, string BodyText)> GetResponseAsync(Uri SendUri, HttpMethod Method, CancellationToken Token = default)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(Method, SendUri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, Token);
            using var resStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(resStream, Encoding.UTF8, true);
            return (response.Headers, await reader.ReadToEndAsync());
        }
    }
}
