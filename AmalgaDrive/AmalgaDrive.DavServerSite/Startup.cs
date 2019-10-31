using System;
using AmalgaDrive.DavServer;
using AmalgaDrive.DavServer.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmalgaDrive.DavServerSite
{
    //  TODO: change the path in appsettings.json to match your environment!!
    public class Startup
    {
        public static DavServerOptions Options { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddDavServer(Configuration, options =>
            {
                // options.ServeHidden = true;
                Options = options;
            });

            services.Configure<KestrelServerOptions>(o =>
            {
                o.AddServerHeader = false;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            // To see these ETW traces, use for example https://github.com/smourier/TraceSpy
            // The guid is an arbitrary value that you'll have to add to TraceSpy's ETW providers.
            // note: this will only works on Windows, but will fail gracefully on other OSes
            loggerFactory.AddEventProvider(new Guid("a3f87db5-0cba-4e4e-b712-439980e59870"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
