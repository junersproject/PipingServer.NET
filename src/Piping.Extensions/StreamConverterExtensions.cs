using Microsoft.Extensions.DependencyInjection;
using Piping.Core.Converters;

namespace Piping.Extensions
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
