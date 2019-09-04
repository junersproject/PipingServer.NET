using System;
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
        readonly IPipingProvider Waiters;
        readonly Encoding Encoding;
        readonly ILogger<PipingController> Logger;
        public PipingController(IPipingProvider Waiters, ILogger<PipingController> Logger, Encoding? Encoding = default)
        {
            this.Waiters = Waiters;
            this.Encoding = Encoding ?? new UTF8Encoding(false);
            this.Logger = Logger;
        }
        [HttpPut("/{**RelativeUri}")]
        [HttpPost("/{**RelativeUri}")]
        public IActionResult Upload(string RelativeUri)
        {
            try
            {
                return Waiters.AddSender(RelativeUri, HttpContext);
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "upload fail.");
                return BadRequest("[ERROR] " + e.Message);
            }
        }

        [HttpGet("/{**RelativeUri}")]
        public IActionResult Download(string RelativeUri)
        {
            try
            {
                return Waiters.AddReceiver(RelativeUri, HttpContext);
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
