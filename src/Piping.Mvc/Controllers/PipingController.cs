using System;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Piping.Core.Models;
using Piping.Mvc.Attributes;

namespace Piping.Mvc.Controllers
{
    [Route("")]
    [ApiController]
    [DisableFormValueModelBinding]
    public class PipingController : ControllerBase
    {
        readonly IPipingProvider Provider;
        readonly Encoding Encoding;
        readonly ILogger<PipingController> Logger;
        public PipingController(IPipingProvider Provider, ILogger<PipingController> Logger, Encoding? Encoding = default)
        {
            this.Provider = Provider;
            this.Encoding = Encoding ?? new UTF8Encoding(false);
            this.Logger = Logger;
        }
        [HttpPut("/{**Path}")]
        [HttpPost("/{**Path}")]
        public IActionResult Upload(string Path)
        {
            try
            {
                var Result = new CompletableStreamResult();
                Provider.SetSender(Path, HttpContext, Result);
                return Result;
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "upload fail.");
                return BadRequest("[ERROR] " + e.Message);
            }
        }

        [HttpGet("/{**Path}")]
        public IActionResult Download(string Path)
        {
            try
            {
                var Result = new CompletableStreamResult();
                Provider.SetReceiver(Path, HttpContext, Result);
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
            var Content = this.Content(Message, $"text/plain; charset={Encoding.WebName}", Encoding);
            Content.StatusCode = 400;
            return Content;
        }
    }
}
