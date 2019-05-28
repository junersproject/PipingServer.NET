using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Piping.Tests
{
    public class RequestTestBase
    {

        protected async Task<(HttpStatusCode StatusCode, HttpResponseHeaders, string BodyText)> GetVersionAsync(Uri BaseUri) => await GetResponseAsync(new Uri(BaseUri.ToString().TrimEnd('/') + "/version"), HttpMethod.Get);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <param name="SendUri"></param>
        /// <param name="Method"></param>
        /// <returns></returns>
        protected async Task<(HttpStatusCode StatusCode, HttpResponseHeaders Headers, string BodyText)> GetResponseAsync(Uri SendUri, HttpMethod Method, CancellationToken Token = default)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(Method, SendUri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, Token);
            using var resStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(resStream, Encoding.UTF8, true);
            return (response.StatusCode, response.Headers, await reader.ReadToEndAsync());
        }

        protected async Task PipingServerPutAndGetMessageSimple(Uri SendUri, string message, CancellationToken Token = default)
        {
            var sender = Task.Run(async () =>
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromHours(6)
                };
                using var request = new HttpRequestMessage(HttpMethod.Put, SendUri)
                {
                    Content = new StringContent(message, Encoding.UTF8, "text/plain"),
                };

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
                }
                finally
                {
                    foreach (var (Key, Value) in request.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
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
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromHours(6)
                };
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

    }
}
