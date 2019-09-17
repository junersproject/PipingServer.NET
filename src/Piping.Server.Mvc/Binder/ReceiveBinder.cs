using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Piping.Server.Core;
using Piping.Server.Core.Converters;

namespace Piping.Server.Mvc.Binder
{
    public class ReceiveBinder : IModelBinder
    {
        readonly ILogger<ReceiveBinder> Logger;
        public ReceiveBinder(ILogger<ReceiveBinder> Logger)
            => (this.Logger) = (Logger);

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(Models.ReceiveData))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }
            try
            {
                var Key = new RequestKey(bindingContext.HttpContext.Request.Path, bindingContext.HttpContext.Request.Query);
                var Receive = new Models.ReceiveData(Key);
                bindingContext.Result = ModelBindingResult.Success(Receive);
            }
            catch (Exception)
            {
                bindingContext.Result = ModelBindingResult.Failed();
            }
            return Task.CompletedTask;
        }
    }
}
