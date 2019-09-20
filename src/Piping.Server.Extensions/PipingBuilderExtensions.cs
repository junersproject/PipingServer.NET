using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Piping.Server.Core.Converters;
using Piping.Server.Core.Pipes;
using Piping.Server.Mvc;
using Piping.Server.Mvc.Infrastructure;

namespace Piping.Server.Extensions
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
        public static IPipingBuilder UseDefaultStore(this IPipingBuilder self)
        {
            self.Services.TryAddSingleton<IPipingStore, PipingStore>();
            return self;
        }
        public static IPipingBuilder AddExector<T>(this IPipingBuilder self)
            where T : class, IActionResultExecutor<PipelineStreamResult>
        {
            self.Services.AddTransient<IActionResultExecutor<PipelineStreamResult>, T>();
            return self;
        }
        public static IPipingBuilder UseDefaultExector(this IPipingBuilder self)
        {
            self.Services.TryAddTransient<IActionResultExecutor<PipelineStreamResult>, CompletableStreamResultExecutor>();
            return self;
        }
    }
}
