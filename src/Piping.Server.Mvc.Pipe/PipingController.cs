using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Piping.Server.Core;
using Piping.Server.Core.Options;
using Piping.Server.Core.Pipes;
using Piping.Server.Mvc.Attributes;
using Piping.Server.Mvc.Binder;
using Piping.Server.Mvc.Models;
using static Piping.Server.Mvc.Pipe.Properties.Resources;

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
            if (GetValidateResult() is IActionResult ErrorResult)
                return ErrorResult;
            var Token = HttpContext.RequestAborted;
            try
            {
                var Path = HttpContext.Request.Path + HttpContext.Request.QueryString;
                var Result = new PipelineStreamResult();
                var Send = await Store.GetSenderAsync(Key, Token);
                await Send.ConnectionAsync(Sender.GetResultAsync(), Result, Token);
                return Result;
            }
            catch (OperationCanceledException e)
            {
                if (Token.IsCancellationRequested)
                {
                    Logger.LogInformation(e.Message);
                    return BadRequest(string.Format(CancelMessage, e.Message));
                }
                Logger.LogInformation(ConnectionTimeout);
                return BadRequest(string.Format(TimeoutMessage,ConnectionTimeout));
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, UploadFail);
                return BadRequest(string.Format(ErrorMessage, e.Message));
            }
        }

        [HttpGet("/{**Path}")]
        public async Task<IActionResult> Download([ModelBinder(typeof(RequestKeyBinder))] RequestKey Key)
        {
            if (GetValidateResult() is IActionResult ErrorResult)
                return ErrorResult;
            var Token = HttpContext.RequestAborted;
            try
            {
                var Path = HttpContext.Request.Path + HttpContext.Request.QueryString;
                var Result = new PipelineStreamResult();
                var Receive = await Store.GetReceiveAsync(Key, Token);
                await Receive.ConnectionAsync(Result, Token);
                return Result;
            }
            catch (OperationCanceledException e)
            {
                if (Token.IsCancellationRequested)
                    return BadRequest(string.Format(CancelMessage, e.Message));
                return BadRequest(string.Format(TimeoutMessage, ConnectionTimeout));
            }
            catch (InvalidOperationException e)
            {
                Logger.LogError(e, DownloadFail);
                return BadRequest(string.Format(ErrorMessage, e.Message));
            }
        }
        IActionResult? GetValidateResult()
        {
            if (!ModelState.IsValid)
            {
                if (!(ModelState.Values.FirstOrDefault(v => v.ValidationState == ModelValidationState.Invalid)?.Errors?.FirstOrDefault() is ModelError me))
                    return BadRequest();
                if (me?.Exception is Exception e)
                {
                    Logger.LogError(e, ValidateFail);
                    return BadRequest(string.Format(ErrorMessage, e.Message));
                }
                if (me?.ErrorMessage is string message)
                {
                    Logger.LogError(message, ValidateFail);
                    return BadRequest(string.Format(ErrorMessage, message));
                }
            }
            return null;
        }
        static readonly Dictionary<string, string> DefaultOptions = new Dictionary<string, string>
        {
            { "Access-Control-Allow-Origin", "*" },
            { "Access-Control-Allow-Methods", "GET, HEAD, POST, PUT, OPTIONS" },
            { "Access-Control-Allow-Headers", "Content-Type, Content-Disposition" },
            { "Access-Control-Max-Age", "86400" },
        };
        [HttpOptions("/")]
        public IActionResult Options()
        {
            var Response = HttpContext.Response;
            Response.StatusCode = 200;
            foreach (var kv in DefaultOptions)
                Response.Headers.Add(kv.Key, kv.Value);
            if (Option.EnableContentOfHeadMethod)
                Response.Headers["Access-Control-Max-Age"] = "-1";
            return new EmptyResult();
        }
        [HttpOptions("/{**Path}")]
        public async ValueTask<IActionResult> Options([ModelBinder(typeof(RequestKeyBinder))]RequestKey Key)
        {
            var Token = HttpContext.RequestAborted;
            if (Option.EnableContentOfHeadMethod)
                return Options();
            var AccessControlAllowMethods = await Store.GetOptionMethodsAsync(Key, Token);
            var Headers = HttpContext.Response.Headers;
            Headers.Add("Access-Control-Allow-Origin", "*");
            Headers.Add("Access-Control-Allow-Methods", string.Join(", ", AccessControlAllowMethods));
            Headers.Add("Access-Control-Max-Age", "-1");
            throw new NotImplementedException();
        }

        const string BadRequestMimeTypeFormat = "text/plain; charset={0}";
        protected ContentResult BadRequest(string Message)
        {
            var Content = this.Content(Message, string.Format(BadRequestMimeTypeFormat, Option.Encoding.WebName), Option.Encoding);
            Content.StatusCode = 400;
            return Content;
        }
    }
}
