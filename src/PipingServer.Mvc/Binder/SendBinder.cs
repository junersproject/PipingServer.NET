using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using PipingServer.Core.Converters;

namespace PipingServer.Mvc.Binder
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
                throw new InvalidOperationException($"not support bind type : {bindingContext.ModelType.FullName}");
            try
            {
                var Sender = new Models.SendData();
                Sender.SetResult(Converters.GetDataAsync(bindingContext.HttpContext.Request, bindingContext.HttpContext.RequestAborted, Logger));
                bindingContext.Result = ModelBindingResult.Success(Sender);
            }
            catch (Exception e)
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, e.Message);
            }
            return Task.CompletedTask;
        }
    }
}
