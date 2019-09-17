using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Piping.Server.Core;

namespace Piping.Server.Mvc.Binder
{
    public class RequestKeyBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));
            if (bindingContext.ModelType != typeof(RequestKey))
                throw new InvalidOperationException($"not support bind type {bindingContext.ModelType.FullName}");
            try
            {
                var Key = new RequestKey(bindingContext.HttpContext.Request.Path, bindingContext.HttpContext.Request.Query);
                bindingContext.Result = ModelBindingResult.Success(Key);
            }
            catch (Exception e)
            {
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.TryAddModelException(bindingContext.ModelName, e);
            }
            return Task.CompletedTask;
        }
    }
}
