using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace AmalgaDrive.DavServerSite
{
    //  TODO: change the path in appsettings.json to match your environment!!
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
    }
}
