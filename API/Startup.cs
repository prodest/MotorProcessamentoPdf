using API.Shared.Filters;
using Business.Core;
using Business.Core.ICore;
using Infrastructure.Repositories;
using Infrastructure.Repositories.IRepositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prodest.HealthCheck;

namespace API
{
    public class Startup
    {
        private readonly IHostEnvironment _env;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;

            _env = env;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IArquivoRepository, ArquivoRepository>();
            services.AddTransient<ICarimboCore, CarimboCore>();
            services.AddTransient<ITransformaPdfCore, TransformaPdfCore>();
            services.AddTransient<IAssinaturaDigitalCore, AssinaturaDigitalCore>();
            services.AddControllers(options =>
                options.Filters.Add(new HttpResponseExceptionFilter())
            );

            services.AddSwaggerGen();

            services.AddCustomHealthChecks(new CustomHealthCheckOptions
            {
                ContentRootFileProvider = _env.ContentRootFileProvider
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Motor de Processamento de PDF - V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCustomHealthChecks();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
