using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Piping.Attributes;
using Piping.Streams;

namespace Piping.Controllers
{
    [Route("")]
    [ApiController]
    [DisableFormValueModelBinding]
    public class PipingController : ControllerBase
    {
        readonly ILogger<PipingController> logger;
        readonly ILoggerFactory loggerFactory;
        const string DICTIONARY = "DICTIONARY";
        /// <summary>
        /// グローバル辞書
        /// </summary>
        private IWaiterDictionary pathToUnestablishedPipe { get; }
        internal Version GetVersion() => GetType().Assembly.GetName().Version;
        internal IReadOnlyDictionary<string, Func<IActionResult>> NAME_TO_RESERVED_PATH;
        private Encoding Encoding = new UTF8Encoding(false);
        public PipingController(ILoggerFactory loggerFactory, IWaiterDictionary pathToUnestablishedPipe)
        {
            this.loggerFactory = loggerFactory;
            logger = loggerFactory.CreateLogger<PipingController>();
            this.pathToUnestablishedPipe = pathToUnestablishedPipe;
            NAME_TO_RESERVED_PATH = new Dictionary<string, Func<IActionResult>>
            {
                { DefaultPath.Root, Index},
                { DefaultPath.Favicon, Favicon },
                { DefaultPath.Help, Help },
                { DefaultPath.Robots, Robots },
                { DefaultPath.Version, Version },
            };
        }
        [HttpPut("/{**RelativeUri}")]
        [HttpPost("/{**RelativeUri}")]
        public async Task<IActionResult> UploadAsync(string RelativeUri)
        {
            if (HttpContext.Request.Body.CanSeek)
                HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            RelativeUri = "/" + RelativeUri.TrimStart('/').ToLower();
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out _))
                return BadRequest($"[ERROR] Cannot send to a reserved path '{RelativeUri}'. (e.g. '/mypath123')\n");
            var Key = new RequestKey(RelativeUri);
            if (Key.Receivers <= 0)
                return BadRequest($"[ERROR] n should > 0, but n = ${Key.Receivers}.\n");
            logger?.LogInformation(string.Join(" ",pathToUnestablishedPipe.Select(v => $"{ v.Key }:{v.Value}")));

            // If the path connection is connecting
            // Get unestablished pipe
            if (!pathToUnestablishedPipe.TryGetValue(Key.LocalPath, out var waiter))
            {
                waiter = new Waiters(Key.Receivers, loggerFactory);
                pathToUnestablishedPipe[Key.LocalPath] = waiter;
            }
            if (waiter.IsEstablished)
                return BadRequest($"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            try
            {
                return await waiter.AddSenderAsync(Key, HttpContext, Encoding, 1024);
            }catch(InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("/{**RelativeUri}")]
        public async Task<IActionResult?> DownloadAsync(string RelativeUri)
        {
            var Context = HttpContext;
            RelativeUri = "/" + RelativeUri.TrimStart('/').ToLower();
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out var Generator))
                return Generator();
            var Key = new RequestKey(RelativeUri);
            if (Key.Receivers <= 0)
                return BadRequest($"[ERROR] n should > 0, but n = {Key.Receivers}.\n");
            if (!pathToUnestablishedPipe.TryGetValue(Key.LocalPath, out var waiter))
            {
                waiter = new Waiters(Key.Receivers, loggerFactory);
                pathToUnestablishedPipe[Key.LocalPath] = waiter;
            }
            if (waiter.IsEstablished)
                return BadRequest($"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            try
            {
                var Result = await waiter.AddReceiverAsync(Context);
                Result.OnFinally += (self, args) =>
                {
                    waiter.RemoveReceiver(Result);
                    Remove();
                };
                return Result;
            }
            catch (Exception e)
            {
                Remove();
                return BadRequest(e.Message);
            }
            void Remove()
            {
                if (waiter.ReceiversIsEmpty)
                {
                    pathToUnestablishedPipe.Remove(Key.LocalPath);
                    waiter.Dispose();
                }
            }
        }
        [HttpGet("/test")]
        public IActionResult TestAsync()
        {
            var Context = HttpContext;
            var Token = Context.RequestAborted;
            var Stream = new CompletableQueueStream();
            var Result = new CompletableStreamResult(loggerFactory.CreateLogger<CompletableStreamResult>())
            {
                Stream = Stream,
                ContentType = $"text/plain; charset={Encoding.WebName}",
            };
            Task.Run(async() =>
            {;
                using var l = logger.BeginLogInformationScope("async TaskAsync Run");
                foreach (var number in Enumerable.Range(1, 500))
                {
                    if (Token.IsCancellationRequested)
                        return;
                    using (var writer = new StreamWriter(Context.Response.Body, Encoding, 1024, true))
                        await writer.WriteLineAsync($"[info] call number {number} {DateTime.Now:yyyy/MM/dd hh:mm:ss}".AsMemory(), Token);
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), Token);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
                Stream.CompleteAdding();
            });
            return Result;
        }
        [HttpGet("/")]
        public IActionResult Index() => Content(Properties.Resources.index, $"text/html; charset={Encoding.WebName}", Encoding);
        [HttpGet(DefaultPath.Help)]
        public IActionResult Help()
        {
            var Request = HttpContext.Request;
            var RequestBaseUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            return Content(GetHelpText(RequestBaseUri, GetVersion()), $"text/plain; chaset={Encoding.WebName}", Encoding);
        }
        internal static string GetHelpText(string url, Version version)
            => $@"Help for piping-server {version.ToString()}
(Repository: https://github.com/nwtgck/piping-server)

======= Get  =======
curl {url}/mypath

======= Send =======
# Send a file
curl -T myfile {url}/mypath

# Send a text
echo 'hello!' | curl -T - {url}/mypath

# Send a directory (zip)
zip -q -r - ./mydir | curl -T - {url}/mypath

# Send a directory (tar.gz)
tar zfcp - ./mydir | curl -T - {url}/mypath

# Encryption
## Send
cat myfile | openssl aes-256-cbc | curl -T - {url}/mypath
## Get
curl {url}/mypath | openssl aes-256-cbc -d";

        [HttpGet(DefaultPath.Version)]
        public IActionResult Version() => Content(GetVersion().ToString(), "text/plain");
        [HttpOptions()]
        public IActionResult Options()
        {
            var Response = HttpContext.Response;
            Response.StatusCode = 200;
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, HEAD, POST, PUT, OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Content-Disposition");
            Response.Headers.Add("Access-Control-Max-Age", "86400");
            return new EmptyResult();
        }
        [HttpGet(DefaultPath.Favicon)]
        public IActionResult Favicon()
        {
            return NotFound();
        }
        [HttpGet(DefaultPath.Robots)]
        public IActionResult Robots()
        {
            return NotFound();
        }
        protected IActionResult BadRequest(string Message)
        {
            var Content = this.Content(Message, $"text/plain; charset={Encoding.WebName}", Encoding);
            Content.StatusCode = 400;
            return Content;
        }
    }
}
