using API.Shared.Filters;
using Business.Core;
using Business.Core.ICore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prodest.HealthCheck;
using Elastic.Apm.AspNetCore;
using API.StartupConfigurations;
using Infrastructure.Repositories;
using Infrastructure;

namespace API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private readonly IHostEnvironment _env;

        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ICarimboCore, CarimboCore>();
            services.AddScoped<ITransformaPdfCore, TransformaPdfCore>();
            services.AddScoped<IAssinaturaDigitalCore, AssinaturaDigitalCore>();
            services.AddScoped<IExtracaoCore, ExtracaoCore>();
            services.AddScoped<IApiRepository, ApiRepository>();
            services.AddScoped<JsonData, JsonData>();

            services.ConfigurarAutomapper();

            // configurando o HttpClientFactory
            services.AddHttpClient();
            services.AddControllers(options =>
                options.Filters.Add(new HttpResponseExceptionFilter())
            );

            services.AddSwaggerGen();

            services.AddCustomHealthChecks(new CustomHealthCheckOptions
            {
                ContentRootFileProvider = _env.ContentRootFileProvider
            });

            //services.Configure<KestrelServerOptions>(options =>
            //{
            //    options.Limits.MaxRequestBodySize = int.MaxValue; // if don't set default value is: 30 MB
            //});

            services.Configure<IISServerOptions>(options =>
            {
                options.MaxRequestBodySize = int.MaxValue;
            });

            services.Configure<FormOptions>(x =>
            {
                x.ValueLengthLimit = int.MaxValue;
                x.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
                x.MultipartHeadersLengthLimit = int.MaxValue;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            app.UseElasticApm(Configuration);
            app.UseCors(options => options.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

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

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCustomHealthChecks();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
