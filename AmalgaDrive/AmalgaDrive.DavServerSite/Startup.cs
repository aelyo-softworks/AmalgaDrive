using System;
using AmalgaDrive.DavServer;
using AmalgaDrive.DavServer.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AmalgaDrive.DavServerSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.AllowEmptyInputInBodyModelBinding = true;
                options.OutputFormatters.RemoveType<JsonOutputFormatter>();
                options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
            });
            services.AddDavServer(Configuration);
            services.Configure<KestrelServerOptions>(o =>
            {
                o.AddServerHeader = false;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // This will only works on Windows, but will fail gracefully on other OSes
            // The guid is an arbitrary value that you can define in TraceSpy's ETW providers: https://github.com/smourier/TraceSpy
            loggerFactory.AddEventProvider(new Guid("a3f87db5-0cba-4e4e-b712-439980e59870"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
