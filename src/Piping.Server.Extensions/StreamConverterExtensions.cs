using Microsoft.Extensions.DependencyInjection;
using Piping.Server.Core.Converters;

namespace Piping.Server.Extensions
{
    public static class StreamConverterExtensions
    {
        public static IServiceCollection UseDefaultStreamConverter(this IServiceCollection services)
        {
            services.AddTransient<IStreamConverter, MultipartStreamConverter>();
            services.AddTransient<IStreamConverter, DefaultStreamConverter>();
            return services;
        }
    }
}
