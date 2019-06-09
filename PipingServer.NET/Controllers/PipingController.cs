using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Piping.Controllers
{
    [Route("")]
    [ApiController]
    public class PipingController : ControllerBase
    {
        readonly ILogger<PipingController> logger;
        const string DICTIONARY = "DICTIONARY";
        /// <summary>
        /// グローバル辞書
        /// </summary>
        private IWaiterDictionary pathToUnestablishedPipe { get; }
        internal Version GetVersion() => GetType().Assembly.GetName().Version;
        internal IReadOnlyDictionary<string, Func<IActionResult>> NAME_TO_RESERVED_PATH;
        private Encoding Encoding = new UTF8Encoding(false);
        public PipingController(ILogger<PipingController> logger, IWaiterDictionary pathToUnestablishedPipe)
        {
            this.logger = logger;
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
        public IActionResult Upload(string RelativeUri)
        {
            RelativeUri = "/" + RelativeUri.TrimStart('/').ToLower();
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out _))
                return BadRequest($"[ERROR] Cannot send to a reserved path '{RelativeUri}'. (e.g. '/mypath123')\n");
            var Key = new RequestKey(RelativeUri);
            if (Key.Receivers <= 0)
                return BadRequest($"[ERROR] n should > 0, but n = ${Key.Receivers}.\n");
            logger?.LogTrace(string.Join(" ",pathToUnestablishedPipe.Select(v => $"{ v.Key }:{v.Value}")));

            // If the path connection is connecting
            // Get unestablished pipe
            if (!pathToUnestablishedPipe.TryGetValue(Key.LocalPath, out var waiter))
            {
                waiter = new Waiters(Key.Receivers);
                pathToUnestablishedPipe[Key.LocalPath] = waiter;
            }
            if (waiter.IsEstablished)
                return BadRequest($"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            try
            {
                var ResponseStream = waiter.AddSender(Key, HttpContext, Encoding, 1024);
                return new FileStreamResult(ResponseStream, HttpContext.Response.Headers["Content-Type"]);
            }catch(InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("/{**RelativeUri}")]
        public async Task<IActionResult> DownloadAsync(string RelativeUri)
        {
            var Context = HttpContext;
            RelativeUri = "/" + RelativeUri.TrimStart('/').ToLower();
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out var Generator))
                return Generator();
            var ResponseStream = new MemoryStream();
            var Key = new RequestKey(RelativeUri);
            if (Key.Receivers <= 0)
                return BadRequest($"[ERROR] n should > 0, but n = {Key.Receivers}.\n");
            if (!pathToUnestablishedPipe.TryGetValue(Key.LocalPath, out var waiter))
            {
                waiter = new Waiters(Key.Receivers);
                pathToUnestablishedPipe[Key.LocalPath] = waiter;
            }
            if (waiter.IsEstablished)
                return BadRequest($"[ERROR] Connection on '{RelativeUri}' has been established already.\n");
            try
            {
                var stream = new CloseRegisterStream(await waiter.AddReceiverAsync(Context));
                stream.Closed += (obj, args) =>
                {
                    waiter.UnRegisterReceiver(Context);
                    if (waiter.ReceiversIsEmpty)
                    {
                        pathToUnestablishedPipe.Remove(Key.LocalPath);
                        waiter.Dispose();
                    }
                };
                return File(stream, Context.Response.Headers["Content-Type"]);
            }
            catch (Exception e)
            {
                waiter.UnRegisterReceiver(Context);
                return BadRequest(e.Message);
            }
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
