using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PipingServer.App.Tests
{
    public abstract class RequestTestBase
    {

        protected async Task<(HttpStatusCode StatusCode, HttpResponseHeaders Headers, HttpContentHeaders Cheaders, string BodyText)> GetVersionAsync(Uri BaseUri) => await GetResponseAsync(new Uri(BaseUri.ToString().TrimEnd('/') + "/version"), HttpMethod.Get);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <param name="SendUri"></param>
        /// <param name="Method"></param>
        /// <returns></returns>
        protected async Task<(HttpStatusCode StatusCode, HttpResponseHeaders Headers, HttpContentHeaders Cheaders, string BodyText)> GetResponseAsync(Uri SendUri, HttpMethod Method, CancellationToken Token = default)
        {
            using var client = new HttpClient();
            using var request = new HttpRequestMessage(Method, SendUri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, Token);
            using var resStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(resStream, Encoding.UTF8, true);
            return (response.StatusCode, response.Headers, response.Content.Headers, await reader.ReadToEndAsync());
        }

        protected async Task PutAndGetTextMessageSimple(Uri SendUri, string message, CancellationToken Token = default)
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
                    foreach (var (Key, Value) in request.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENT CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    Trace.WriteLine("[SENDER REQUEST] [END]");
                }
                Trace.WriteLine("[SENDER RESPONSE] [START]");
                try
                {
                    Trace.WriteLine($"[SENDER'S RESPONSE STATUS CODE] : {response.StatusCode}");
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENDER'S RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var outstream = await response.Content.ReadAsStreamAsync();
                    foreach (var (Key, Value) in response.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENDER'S RESPONSE CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var outstreamDispose = Token.Register(() => outstream.Dispose());
                    using var reader = new StreamReader(outstream, Encoding.UTF8, false, 1024, true);
                    string? Line;
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
                    Trace.WriteLine($"[RESPONSE STATUS CODE] : {response.StatusCode}");
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var stream = await response.Content.ReadAsStreamAsync();
                    foreach (var (Key, Value) in response.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[RESPONSE CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var streamDispose = Token.Register(() => stream.Dispose());
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                    string? Line;
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
        protected async Task PostAndGetMultipartTestMessageSimple(Uri SendUri, string message1, CancellationToken Token = default)
        {

            var sender = Task.Run(async () =>
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromHours(6)
                };
                using var request = new HttpRequestMessage(HttpMethod.Post, SendUri)
                {
                    Content = new MultipartFormDataContent
                    {
                        { new StringContent(message1, Encoding.UTF8, "text/plain"){
                            Headers = {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data"){
                                    Name = "input_text",
                                },
                            },
                        }, "input_text"},
                    },
                };

                foreach (var (Key, Value) in request.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                    Trace.WriteLine($"[SEND HEADER] : {Key} : [{string.Join(", ", Value)}]");
                HttpResponseMessage response;
                Trace.WriteLine("[SENDER REQUEST] [START]");
                try
                {
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
                }
                finally
                {
                    foreach (var (Key, Value) in request.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    foreach (var (Key, Value) in request.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENT CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    Trace.WriteLine("[SENDER REQUEST] [END]");
                }
                Trace.WriteLine("[SENDER RESPONSE] [START]");
                try
                {
                    Trace.WriteLine($"[SENDER'S RESPONSE STATUS CODE] : {response.StatusCode}");
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENDER'S RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var outstream = await response.Content.ReadAsStreamAsync();
                    foreach (var (Key, Value) in response.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENDER'S RESPONSE CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var outstreamDispose = Token.Register(() => outstream.Dispose());
                    using var reader = new StreamReader(outstream, Encoding.UTF8, false, 1024, true);
                    string? Line;
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
                    Trace.WriteLine($"[RESPONSE STATUS CODE] : {response.StatusCode}");
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var stream = await response.Content.ReadAsStreamAsync();
                    foreach (var (Key, Value) in response.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[RESPONSE CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var streamDispose = Token.Register(() => stream.Dispose());
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                    string? Line;
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
            Assert.AreEqual(message1, await receiver);
        }
        protected async Task PostAndGetMultipartTestFileSimple(Uri SendUri, string FileName, string MediaType, byte[] FileData, CancellationToken Token = default)
        {

            var sender = Task.Run(async () =>
            {
                using var client = new HttpClient
                {
                    Timeout = TimeSpan.FromHours(6)
                };
                using var request = new HttpRequestMessage(HttpMethod.Post, SendUri)
                {
                    Content = new MultipartFormDataContent
                    {
                        { new ByteArrayContent(FileData){
                            Headers =
                            {
                                ContentType = new MediaTypeHeaderValue(MediaType),
                            }
                        }, "input_file", FileName },
                    },
                };

                foreach (var (Key, Value) in request.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                    Trace.WriteLine($"[SEND HEADER] : {Key} : [{string.Join(", ", Value)}]");
                HttpResponseMessage response;
                Trace.WriteLine("[SENDER REQUEST] [START]");
                try
                {
                    response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
                }
                finally
                {
                    foreach (var (Key, Value) in request.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    foreach (var (Key, Value) in request.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENT CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    Trace.WriteLine("[SENDER REQUEST] [END]");
                }
                Trace.WriteLine("[SENDER RESPONSE] [START]");
                try
                {
                    Trace.WriteLine($"[SENDER'S RESPONSE STATUS CODE] : {response.StatusCode}");
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENDER'S RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var outstream = await response.Content.ReadAsStreamAsync();
                    foreach (var (Key, Value) in response.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[SENDER'S RESPONSE CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var outstreamDispose = Token.Register(() => outstream.Dispose());
                    using var reader = new StreamReader(outstream, Encoding.UTF8, false, 1024, true);
                    string? Line;
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
                string? ContentType;
                string? FileName;
                try
                {
                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, Token);
                    using var responseDispose = Token.Register(() => response.Dispose());
                    Trace.WriteLine($"[RESPONSE STATUS CODE] : {response.StatusCode}");
                    foreach (var (Key, Value) in response.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[RESPONSE HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    using var stream = await response.Content.ReadAsStreamAsync();
                    foreach (var (Key, Value) in response.Content.Headers.Where(v => v.Value.Any()).Select(kv => (kv.Key, kv.Value)))
                        Trace.WriteLine($"[RESPONSE CONTENT HEADER] : {Key} : [{string.Join(", ", Value)}]");
                    ContentType = response.Content.Headers.ContentType.MediaType;
                    FileName = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName ?? null;
                    using var streamDispose = Token.Register(() => stream.Dispose());
                    var buffer = new byte[1024];
                    var Bytes = new List<byte>();
                    int Count;
                    while (0 < (Count = await stream.ReadAsync(buffer, 0, buffer.Length, Token)))
                    {
                        Trace.WriteLine($"[RECEIVE BYTES] : {string.Join(" ", buffer.Take(Count).Select(v => $"{v:X2}"))}");
                        Bytes.AddRange(buffer.Take(Count));
                    }
                    return (Bytes.ToArray(), ContentType, FileName);
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
            var (bytes, _MediaType, _FileName) = await receiver;
            Assert.AreEqual(MediaType, _MediaType);
            Assert.AreEqual(FileName, _FileName);
            CollectionAssert.AreEqual(FileData, bytes);
        }
    }
}
