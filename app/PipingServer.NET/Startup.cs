using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Piping.Core.Converters;
using Piping.Core.Pipes;
using Piping.Mvc;
using Piping.Mvc.Infrastructure;

namespace Piping
{
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<PipingOptions>(Configuration.GetSection("PipingOptions"));
            services.AddControllers();
            services.AddTransient<IActionResultExecutor<CompletableStreamResult>, CompletableStreamResultExecutor>();
            services.AddSingleton<IPipingProvider, PipingProvider>();
            services.AddTransient<CompletableStreamResult>();
            services.AddTransient<Encoding>(_ => new UTF8Encoding(false));
            services.AddTransient<IStreamConverter, MultipartStreamConverter>();
            services.AddTransient<IStreamConverter, DefaultStreamConverter>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(routes =>
            {
                routes.MapControllers();
            });
        }
    }
}
