using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Piping.Mvc.Attributes
{
    /// <summary>
    /// <see cref="https://docs.microsoft.com/ja-jp/aspnet/core/mvc/models/file-uploads?view=aspnetcore-2.2#uploading-large-files-with-streaming"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var factories = context.ValueProviderFactories;

            factories.RemoveType<FormValueProviderFactory>();
            factories.RemoveType<JQueryFormValueProviderFactory>();
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}
