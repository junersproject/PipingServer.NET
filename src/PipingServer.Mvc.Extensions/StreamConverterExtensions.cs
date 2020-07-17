using Microsoft.Extensions.DependencyInjection;
using PipingServer.Mvc.Converters;

namespace PipingServer.Mvc.Extensions
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
