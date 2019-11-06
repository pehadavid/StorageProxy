using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace StorageProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json", optional:true, reloadOnChange: true)
                .AddJsonFile("secrets/appsettings.json", optional: true)
                .AddEnvironmentVariables()
           .Build();

            var builder = WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                .UseStartup<Startup>()
                .UseKestrel(options => { });

            var host = builder.Build();
            host.Run();

           
        }
    }
}