using APIItextSharp.StartupConfigurations;
using BusinessItextSharp.Core;
using Elastic.Apm.AspNetCore;
using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Prodest.HealthCheck;

namespace APIItextSharp
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
            services.AddScoped<IAssinaturaDigitalCore, AssinaturaDigitalCore>();
            services.AddScoped<JsonData, JsonData>();
            services.AddHttpClient();

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "APIItextSharp", Version = "v1" });
            });
            services.AddCustomHealthChecks(new CustomHealthCheckOptions
            {
                ContentRootFileProvider = _env.ContentRootFileProvider
            });

            services.ConfigurarAutomapper();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            app.UseElasticApm(Configuration);
            app.UseCors(options => options.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "APIItextSharp v1"));
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
