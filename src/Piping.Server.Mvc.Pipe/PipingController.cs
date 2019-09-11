using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Piping.Server.Core.Pipes;
using Piping.Server.Mvc.Attributes;
using Piping.Server.Mvc.Models;

namespace Piping.Server.Mvc.Pipe
{
    [Route("")]
    [ApiController]
    [RouteValueOnlyBinding]
    public class PipingController : ControllerBase
    {
        readonly IPipingProvider Provider;
        readonly PipingOptions Option;
        readonly ILogger<PipingController> Logger;
        public PipingController(IPipingProvider Provider, ILogger<PipingController> Logger, IOptions<PipingOptions> Options)
        {
            this.Provider = Provider;
            Option = Options.Value;
            this.Logger = Logger;
        }
        [HttpPut("/{**Path}")]
        [HttpPost("/{**Path}")]
        public async Task<IActionResult> Upload(string Path, SendData Sender)
        {
            try
            {
                var Result = new CompletableStreamResult();
                await Provider.SetSenderAsync(Path, Sender.GetResultAsync(), HttpContext, Result);
                return Result;
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "upload fail.");
                return BadRequest("[ERROR] " + e.Message);
            }
        }

        [HttpGet("/{**Path}")]
        public async Task<IActionResult> Download(string Path)
        {
            try
            {
                var Result = new CompletableStreamResult();
                await Provider.SetReceiverAsync(Path, HttpContext, Result);
                return Result;
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "download fail.");
                return BadRequest("[ERROR] " + e.Message);
            }
        }

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
        protected IActionResult BadRequest(string Message)
        {
            var Content = this.Content(Message, $"text/plain; charset={Option.Encoding.WebName}", Option.Encoding);
            Content.StatusCode = 400;
            return Content;
        }
    }
}
