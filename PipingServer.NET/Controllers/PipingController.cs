using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Piping.Attributes;
using Piping.Models;

namespace Piping.Controllers
{
    [Route("")]
    [ApiController]
    [DisableFormValueModelBinding]
    public class PipingController : ControllerBase
    {
        readonly IWaiters Waiters;
        internal Version GetVersion() => GetType()?.Assembly?.GetName()?.Version ?? throw new InvalidOperationException();
        internal IReadOnlyDictionary<string, Func<IActionResult>> NAME_TO_RESERVED_PATH;
        private Encoding Encoding = new UTF8Encoding(false);
        public PipingController(IWaiters Waiters)
        {
            this.Waiters = Waiters;
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
            
            // If the path connection is connecting
            // Get unestablished pipe
            try
            {
                return Waiters.AddSender(Key, HttpContext, Encoding, 1024);
            }catch(InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("/{**RelativeUri}")]
        public IActionResult Download(string RelativeUri)
        {
            var Context = HttpContext;
            RelativeUri = "/" + RelativeUri.TrimStart('/').ToLower();
            if (NAME_TO_RESERVED_PATH.TryGetValue(RelativeUri, out var Generator))
                return Generator();
            var Key = new RequestKey(RelativeUri);
            try
            {
                return Waiters.AddReceiver(Key, Context);
            }
            catch (Exception e)
            {
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
