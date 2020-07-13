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

namespace PipingServer.App.APITests
{
    public abstract class TestBase
    {
        protected async Task PutAndOneGet(Func<HttpClient> CreateClient, string SendUri = nameof(PutAndOneGet), CancellationToken Token = default)
        {
            var message = "Hello World.";
            Trace.WriteLine($"TARGET URL: {SendUri}");
            var (_, _, _, Version) = await GetVersionAsync(CreateClient);
            Trace.WriteLine($"VERSION: {Version}");
            await PutAndGetTextMessageSimple(CreateClient, SendUri, message, Token: Token);
        }
        protected Version MultipartSupportVersion = new Version(0, 8, 3);
        protected async Task PostAndOneGetTextMultipart(Func<HttpClient> CreateClient, string SendUri = nameof(PostAndOneGetTextMultipart), CancellationToken Token = default)
        {
            var message1 = "Hello World.";
            Trace.WriteLine($"TARGET URL: {SendUri}");
            var (_, _, _, Version) = await GetVersionAsync(CreateClient);
            Trace.WriteLine($"VERSION: {Version}");
            if (new Version(Version) < MultipartSupportVersion)
                throw new AssertInconclusiveException($"Multipart Support Version is {MultipartSupportVersion} or later.");
            await PostAndGetMultipartTestMessageSimple(CreateClient, SendUri, message1, Token: Token);
        }
        protected async Task PostAndOneGetFileMultipart(Func<HttpClient> CreateClient, string SendUri = nameof(PostAndOneGetFileMultipart), CancellationToken Token = default)
        {
            var message = "Hello World.";
            Trace.WriteLine($"TARGET URL: {SendUri}");
            var (_, _, _, Version) = await GetVersionAsync(CreateClient);
            Trace.WriteLine($"VERSION: {Version}");
            if (new Version(Version) < MultipartSupportVersion)
                throw new AssertInconclusiveException($"Multipart Support Version is {MultipartSupportVersion} or later.");
            var FileName = "test.txt";
            var MediaType = "text/plain";
            var FileData = Encoding.UTF8.GetBytes(message);
            await PostAndGetMultipartTestFileSimple(CreateClient, SendUri, FileName, MediaType, FileData, Token);
        }
        [Description("piping-server の /version を取得する")]
        protected async Task GetVersion(Func<HttpClient> CreateClient, CancellationToken Token = default)
        {
            var (Status, Headers, Cheaders, BodyText) = await GetResponseAsync(CreateClient, "version", HttpMethod.Get, Token);
            Trace.WriteLine(Status);
            Trace.WriteLine(Headers);
            Trace.WriteLine(Cheaders);
            Trace.WriteLine(BodyText);
            Assert.AreEqual(HttpStatusCode.OK, Status);
        }

        [Description("piping-server の / を取得する")]
        protected async Task GetRoot(Func<HttpClient> CreateClient, CancellationToken Token = default)
        {
            var (Status, Headers, Cheaders, BodyText) = await GetResponseAsync(CreateClient, "/", HttpMethod.Get, Token);
            Trace.WriteLine(Status);
            Trace.WriteLine(Headers);
            Trace.WriteLine(Cheaders);
            Trace.WriteLine(BodyText);
            Assert.AreEqual(HttpStatusCode.OK, Status);
        }
        [Description("piping-server の ルート を取得する")]
        protected async Task GetRoot2(Func<HttpClient> CreateClient, CancellationToken Token = default)
        {
            var (Status, Headers, Cheaders, BodyText) = await GetResponseAsync(CreateClient, "/", HttpMethod.Get, Token);
            Trace.WriteLine(Status);
            Trace.WriteLine(Headers);
            Trace.WriteLine(Cheaders);
            Trace.WriteLine(BodyText);
            Assert.AreEqual(HttpStatusCode.OK, Status);
        }
        [Description("piping-server の /help の取得を試みる。")]
        protected async Task GetHelp(Func<HttpClient> CreateClient, CancellationToken Token = default)
        {
            var (Status, Headers, Cheaders, BodyText) = await GetResponseAsync(CreateClient, "/help", HttpMethod.Get, Token);
            Trace.WriteLine(Status);
            Trace.WriteLine(Headers);
            Trace.WriteLine(Cheaders);
            Trace.WriteLine(BodyText);
            Assert.AreEqual(HttpStatusCode.OK, Status);
        }
        protected async Task OptionsRoot(Func<HttpClient> CreateClient, CancellationToken Token = default)
        {
            var (Status, Headers, Cheaders, BodyText) = await GetResponseAsync(CreateClient, "/", HttpMethod.Options, Token);
            Trace.WriteLine(Status);
            Trace.WriteLine(Headers);
            Trace.WriteLine(Cheaders);
            Trace.WriteLine(BodyText);
            Assert.AreEqual(HttpStatusCode.OK, Status);
        }
        protected async Task PostRoot(Func<HttpClient> CreateClient, CancellationToken Token = default)
        {
            var (Status, Headers, Cheaders, BodyText) = await GetResponseAsync(CreateClient, "/", HttpMethod.Post, Token);
            Trace.WriteLine(Status);
            Trace.WriteLine(Headers);
            Trace.WriteLine(Cheaders);
            Trace.WriteLine(BodyText);
            Assert.AreEqual(HttpStatusCode.BadRequest, Status);
        }
        /// <summary>
        /// リモート名の解決に失敗
        /// </summary>
        /// <param name="e"></param>
        public void ThrowIfCoundNotResolveRemoteName(HttpRequestException e)
        {
            if (e.HResult == -2146233088)
            {
                Trace.WriteLine(e);
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        /// <summary>
        /// ホストが見つかりません
        /// </summary>
        /// <param name="e"></param>
        public void ThrowIfHostIsUnknown(HttpRequestException e)
        {
            if (e.HResult == -2147467259)
            {
                Trace.WriteLine(e);
                throw new AssertInconclusiveException(e.Message, e);
            }
        }
        readonly Encoding Encoding = Encoding.UTF8;
        private async Task<(HttpStatusCode StatusCode, HttpResponseHeaders Headers, HttpContentHeaders Cheaders, string BodyText)> GetVersionAsync(Func<HttpClient> CreateClient, CancellationToken Token = default) => await GetResponseAsync(CreateClient, "version", HttpMethod.Get, Token);
        private async Task<(HttpStatusCode StatusCode, HttpResponseHeaders Headers, HttpContentHeaders Cheaders, string BodyText)> GetResponseAsync(Func<HttpClient> CreateClient, string SendUri, HttpMethod Method, CancellationToken Token = default)
        {
            using var client = CreateClient();
            using var request = new HttpRequestMessage(Method, SendUri);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, Token);
            using var resStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(resStream, Encoding.UTF8, true);
            return (response.StatusCode, response.Headers, response.Content.Headers, await reader.ReadToEndAsync());
        }

        private async Task PutAndGetTextMessageSimple(Func<HttpClient> CreateClient, string Path, string message, CancellationToken Token = default)
        {
            var sender = Task.Run(async () =>
            {
                using var client = CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Put, Path)
                {
                    Content = new StringContent(message, Encoding, "text/plain"),
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
                    using var reader = new StreamReader(outstream, Encoding, false, 1024, true);
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
                using var client = CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, Path);


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
                    using var reader = new StreamReader(stream, Encoding, false, 1024, true);
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
        private async Task PostAndGetMultipartTestMessageSimple(Func<HttpClient> CreateClient, string Path, string message1, CancellationToken Token = default)
        {

            var sender = Task.Run(async () =>
            {
                using var client = CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, Path)
                {
                    Content = new MultipartFormDataContent
                    {
                        { new StringContent(message1, Encoding, "text/plain"){
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
                    using var reader = new StreamReader(outstream, Encoding, false, 1024, true);
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
                using var client = CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, Path);


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
                    using var reader = new StreamReader(stream, Encoding, false, 1024, true);
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
        private async Task PostAndGetMultipartTestFileSimple(Func<HttpClient> CreateClient, string Path, string FileName, string MediaType, byte[] FileData, CancellationToken Token = default)
        {

            var sender = Task.Run(async () =>
            {
                using var client = CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, Path)
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
                    using var reader = new StreamReader(outstream, Encoding, false, 1024, true);
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
                using var client = CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Get, Path);


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
                    Assert.IsNotNull(response.Content.Headers.ContentType, "RESPONSE CONTENT-TYPE IS NULL.");
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
