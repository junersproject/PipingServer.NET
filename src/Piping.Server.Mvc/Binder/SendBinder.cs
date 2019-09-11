using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Piping.Server.Core;
using Piping.Server.Core.Converters;

namespace Piping.Server.Mvc.Binder
{
    public class SendBinder : IModelBinder
    {
        readonly IEnumerable<IStreamConverter> Converters;
        readonly ILogger<SendBinder> Logger;
        public SendBinder(IEnumerable<IStreamConverter> Converters, ILogger<SendBinder> Logger)
            => (this.Converters, this.Logger) = (Converters, Logger);
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(Models.SendData))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            try
            {
                var Key = new RequestKey(bindingContext.HttpContext.Request.Path + bindingContext.HttpContext.Request.Query);
                var Sender = new Models.SendData(Key);
                Sender.SetResult(Converters.GetDataAsync(bindingContext.HttpContext.Request, bindingContext.HttpContext.RequestAborted, Logger));
                bindingContext.Result = ModelBindingResult.Success(Sender);
            }
            catch (Exception)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            return Task.CompletedTask;
        }
    }
}
