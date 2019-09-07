using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Piping.Extensions
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
