#if NETCORE

using ChristianMoser.WpfInspector.Process32Helper;
using ChristianMoser.WpfInspector.Services;
using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Text;

namespace Process32Helper
{
    public static class Application
    {
        /// <summary>
        /// Static application entry point
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            host.Run();

        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options => { options.ListenLocalhost(8080); })
                .UseUrls("http://localhost:8080")
                .UseNetTcp(8808)
                .UseStartup<Startup>();
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseServiceModel(builder =>
            {
                builder.AddService<ProcessService>();
                builder.AddServiceEndpoint<ProcessService, IProcessService>(new BasicHttpBinding(), "/basichttp");
                builder.AddServiceEndpoint<ProcessService, IProcessService>(new CoreWCF.NetTcpBinding(), "/nettcp");
            });
        }
    }
}
#endif