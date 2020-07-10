using Microsoft.Extensions.DependencyInjection;

namespace Piping.Server.Extensions
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
