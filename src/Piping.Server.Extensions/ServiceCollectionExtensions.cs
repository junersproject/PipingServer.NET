﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Piping.Server.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPiping(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            new PipingBuilder(services)
                .UseDefaultExector()
                .UseDefaultProvider()
                .UseMultipartStreamConverter();
            return services;
        }
        public static IServiceCollection AddPiping(this IServiceCollection services, Action<IPipingBuilder> action)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            var builder = new PipingBuilder(services);
            action.Invoke(builder);
            builder.UseDefaultExector()
                .UseDefaultProvider();
            return services;
        }
    }
}