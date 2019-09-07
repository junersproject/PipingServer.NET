using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Piping.Core.Converters;
using Piping.Core.Pipes;
using Piping.Mvc;
using Piping.Mvc.Infrastructure;

namespace Piping.Extensions
{
    public static class PipingBuilderExtensions
    {
        public static IPipingBuilder AddConverter<T>(this IPipingBuilder self)
            where T : class, IStreamConverter
        {
            self.Services.AddTransient<IStreamConverter, T>();
            return self;
        }
        public static IPipingBuilder UseMultipartStreamConverter(this IPipingBuilder self)
        {
            self.Services.AddTransient<IStreamConverter, MultipartStreamConverter>();
            return self;
        }
        public static IPipingBuilder AddProvider<T>(this IPipingBuilder self)
            where T : class, IPipingProvider
        {
            self.Services.AddSingleton<IPipingProvider, T>();
            return self;
        }
        public static IPipingBuilder UseDefaultProvider(this IPipingBuilder self)
        {
            self.Services.TryAddSingleton<IPipingProvider, PipingProvider>();
            return self;
        }
        public static IPipingBuilder AddExector<T>(this IPipingBuilder self)
            where T : class, IActionResultExecutor<CompletableStreamResult>
        {
            self.Services.AddTransient<IActionResultExecutor<CompletableStreamResult>, T>();
            return self;
        }
        public static IPipingBuilder UseDefaultExector(this IPipingBuilder self)
        {
            self.Services.TryAddTransient<IActionResultExecutor<CompletableStreamResult>, CompletableStreamResultExecutor>();
            return self;
        }
    }
}
