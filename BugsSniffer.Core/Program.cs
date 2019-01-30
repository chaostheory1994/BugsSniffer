using BugsSniffer.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BugsSniffer
{
    class Program
    {

        public static async Task Main(string[] args)
        {
            IConfiguration config = BuildConfiguration();
            ILoggerFactory factory = BuildLoggerFactory();

            using (BugSnifferService service = new BugSnifferService(factory, config).Init())
            {
                Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
                {
                    service.Stop();
                    e.Cancel = true;
                };

                await service.Run();
            }
                
        }

        public static IConfiguration BuildConfiguration()
            => new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

        public static ILoggerFactory BuildLoggerFactory()
#pragma warning disable CS0618 // Type or member is obsolete
            => new LoggerFactory().AddConsole();
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
