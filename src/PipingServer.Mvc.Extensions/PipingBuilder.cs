using Microsoft.Extensions.DependencyInjection;

namespace PipingServer.Mvc.Extensions
{
    public interface IPipingBuilder
    {
        IServiceCollection Services { get; }
    }
    public class PipingBuilder : IPipingBuilder
    {
        public PipingBuilder(IServiceCollection Services)
            => this.Services = Services;
        public IServiceCollection Services { get; }
    }
}
