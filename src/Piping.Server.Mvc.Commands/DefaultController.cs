using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Piping.Server.Mvc.Commands
{
    [Route("")]
    [ApiController]
    public class DefaultController : ControllerBase
    {
        readonly Encoding Encoding;
        readonly ILogger<DefaultController> Logger;
        public DefaultController(ILogger<DefaultController> Logger, Encoding? Encoding = default)
        {
            this.Logger = Logger;
            this.Encoding = Encoding ?? new UTF8Encoding(false);
        }
        internal Version GetVersion() => typeof(Core.RequestKey)?.Assembly?.GetName()?.Version ?? throw new InvalidOperationException();
        /// <summary>
        /// ルートへのアクセス
        /// </summary>
        /// <returns></returns>
        [HttpGet(DefaultPath.Root)]
        public IActionResult Index() => Content(Properties.Resources.Index, $"text/html; charset={Encoding.WebName}", Encoding);
        /// <summary>
        /// ヘルプ
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// バージョン情報
        /// </summary>
        /// <returns></returns>
        [HttpGet(DefaultPath.Version)]
        public IActionResult Version() => Content(GetVersion().ToString(), "text/plain");
        /// <summary>
        /// favicon
        /// </summary>
        /// <returns></returns>
        [HttpGet(DefaultPath.Favicon)]
        public IActionResult Favicon()
        {
            return NotFound();
        }
        /// <summary>
        /// robot.txt
        /// </summary>
        /// <returns></returns>
        [HttpGet(DefaultPath.Robots)]
        public IActionResult Robots()
        {
            return NotFound();
        }
        /// <summary>
        /// エラーアクセスパターン
        /// </summary>
        /// <returns></returns>
        [HttpPost(DefaultPath.Version)]
        [HttpPut(DefaultPath.Version)]
        [HttpPost(DefaultPath.Favicon)]
        [HttpPut(DefaultPath.Favicon)]
        [HttpPost(DefaultPath.Robots)]
        [HttpPut(DefaultPath.Robots)]
        [HttpPost(DefaultPath.Help)]
        [HttpPut(DefaultPath.Help)]
        [HttpPost(DefaultPath.Root)]
        [HttpPut(DefaultPath.Root)]
        public IActionResult ErrorAccess()
        {
            var RelativeUri = HttpContext.Request.Path;
            var Message = $"Cannot send to a reserved path '{RelativeUri}'. (e.g. '/mypath123')";
            Logger.LogError(Message);
            return BadRequest($"[ERROR] {Message}\n");
        }
        protected IActionResult BadRequest(string Message)
        {
            var Content = this.Content(Message, $"text/plain; charset={Encoding.WebName}", Encoding);
            Content.StatusCode = 400;
            return Content;
        }
    }
}
