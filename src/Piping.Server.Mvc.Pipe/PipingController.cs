using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Piping.Server.Core;
using Piping.Server.Core.Pipes;
using Piping.Server.Mvc.Attributes;
using Piping.Server.Mvc.Binder;
using Piping.Server.Mvc.Models;

namespace Piping.Server.Mvc.Pipe
{
    [Route("")]
    [ApiController]
    [RouteValueOnlyBinding]
    public class PipingController : ControllerBase
    {
        readonly IPipingStore Store;
        readonly PipingOptions Option;
        readonly ILogger<PipingController> Logger;
        public PipingController(IPipingStore Store, ILogger<PipingController> Logger, IOptions<PipingOptions> Options)
        {
            this.Store = Store;
            Option = Options.Value;
            this.Logger = Logger;
        }
        [HttpPut("/{**Path}")]
        [HttpPost("/{**Path}")]
        public async Task<IActionResult> Upload([ModelBinder(typeof(RequestKeyBinder))] RequestKey Key, SendData Sender)
        {
            if (!ModelState.IsValid)
            {
                if (!(ModelState.Values.FirstOrDefault()?.Errors?.FirstOrDefault() is ModelError me))
                    return BadRequest();
                if (me?.Exception is Exception e)
                {
                    Logger.LogError(e, "validate fail.");
                    return BadRequest("[ERROR] " + e.Message);
                }
                if (me?.ErrorMessage is string message)
                {
                    Logger.LogError(message, "validate fail.");
                    return BadRequest("[ERROR] " + message);
                }
            }
            try
            {
                var Path = HttpContext.Request.Path + HttpContext.Request.QueryString;
                var Token = HttpContext.RequestAborted;
                var Result = new CompletableStreamResult();
                var Send = await Store.GetSenderAsync(Key, Token);
                await Send.ConnectionAsync(Sender.GetResultAsync(), Result, Token);
                return Result;
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, "upload fail.");
                return BadRequest("[ERROR] " + e.Message);
            }
        }

        [HttpGet("/{**Path}")]
        public async Task<IActionResult> Download([ModelBinder(typeof(RequestKeyBinder))] RequestKey Key)
        {
            if (!ModelState.IsValid)
            {
                if (!(ModelState.Values.FirstOrDefault()?.Errors?.FirstOrDefault() is ModelError me))
                    return BadRequest();
                if (me?.Exception is Exception e)
                {
                    Logger.LogError(e, "validate fail.");
                    return BadRequest("[ERROR] " + e.Message);
                }
                if (me?.ErrorMessage is string message)
                {
                    Logger.LogError(message, "validate fail.");
                    return BadRequest("[ERROR] " + message);
                }
            }
            try
            {
                var Path = HttpContext.Request.Path + HttpContext.Request.QueryString;
                var Token = HttpContext.RequestAborted;
                var Result = new CompletableStreamResult();
                var Receive = await Store.GetReceiveAsync(Key, Token);
                await Receive.ConnectionAsync(Result, Token);
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
        protected ContentResult BadRequest(string Message)
        {
            var Content = this.Content(Message, $"text/plain; charset={Option.Encoding.WebName}", Option.Encoding);
            Content.StatusCode = 400;
            return Content;
        }
    }
}
