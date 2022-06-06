using API.Shared.Filters;
using API.StartupConfigurations;
using Business.Core;
using Business.Core.ICore;
using Elastic.Apm.NetCoreAll;
using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prodest.HealthCheck;
using Prodest.Monitoring.Extensions.DependencyInjection;
using Prodest.Monitoring.Extensions.Monitoring;


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

            services.Configure<IISServerOptions>(iisServerOptions =>
            {
                iisServerOptions.MaxRequestBodySize = int.MaxValue;
            });

            services.Configure<FormOptions>(formOptions =>
            {
                formOptions.ValueCountLimit = int.MaxValue;
                formOptions.ValueLengthLimit = int.MaxValue;
                formOptions.MultipartBodyLengthLimit = int.MaxValue; // if don't set default value is: 128 MB
                formOptions.MultipartHeadersLengthLimit = int.MaxValue;
            });

            services.AddMachineMonitoring(Configuration.GetConnectionString("RedisConnection"), "PDF");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAllElasticApm(Configuration);
            app.UseForwardedHeaders();

            IMachineMonitoring machineMonitoring = app.ApplicationServices.GetService<IMachineMonitoring>();

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
