using System;
using AmalgaDrive.DavServer;
using AmalgaDrive.DavServer.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            // add our file system as a service and configure it
            // TODO: change the path in appsettings.json to match your environment!!
            services.AddFileSystem(Configuration, options =>
            {
                // configure some options here
                // options.ServeHidden = true;
            });

            // from here, we use standard extensions
            services.AddDirectoryBrowser();
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            // To see these ETW traces, use for example https://github.com/smourier/TraceSpy
            // The guid is an arbitrary value that you'll have to add to TraceSpy's ETW providers.
            // note: this will only works on Windows, but will fail gracefully on other OSes
            loggerFactory.AddEventProvider(new Guid("a3f87db5-0cba-4e4e-b712-439980e59870"));

            // allows the user to browse files served by our dav server (currently only in the case of the LocalFileSystem implementation)
            // if this is confured, don't use the RequestPath in the localdrive of course
            var options = Configuration.GetFileSystemDirectoryBrowserOptions();
            if (options != null)
            {
                app.UseDirectoryBrowser(options);
            }

            // from here, we use standard extensions
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
