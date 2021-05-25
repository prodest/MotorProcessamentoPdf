using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IConfigurationRoot appSettings = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            string ambiente = appSettings["ambiente"];

            IHostBuilder hostBuilder = Host
                .CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .UseEnvironment(ambiente);

            return hostBuilder;
        }
    }
}
