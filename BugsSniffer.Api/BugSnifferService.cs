using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpPcap;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BugsSniffer.Api
{
    public class BugSnifferService : IDisposable
    {
        private readonly FileDownloader _fileDownloader;
        private readonly PacketExtractor _packetExtractor;
        private readonly ILogger<BugSnifferService> _logger;
        private readonly IConfiguration _config;
        private ICaptureDevice _device;
        private bool running;

        public int Timeout => int.TryParse(_config["packetTimeout"], out int result) ? result : 0;

        public BugSnifferService(ILoggerFactory factory, IConfiguration config)
        {
            _logger = factory.CreateLogger<BugSnifferService>();
            _config = config;

            _logger.LogInformation("Building HttpClient");
            HttpClient client = new HttpClient();
            string baseAddress = _config["baseAddress"];

            if (!string.IsNullOrWhiteSpace(baseAddress))
            {
                _logger.LogInformation($"Setting base address to {baseAddress}");
                client.BaseAddress = new Uri(baseAddress);
            }

            _fileDownloader = new FileDownloader(factory, config, client);
            _packetExtractor = new PacketExtractor(factory);
            running = false;
        }

        public BugSnifferService Init()
        {
            _logger.LogInformation($"Bugs! Sniffer Service using SharpPcap {SharpPcap.Version.VersionString}");
            _logger.LogInformation("Reading computer's network devices");

            CaptureDeviceList devices = CaptureDeviceList.Instance;

            if(devices.Count < 1)
            {
                _logger.LogError("COULD NOT FIND ANY DEVICES!!!!");
                throw new ArgumentNullException();
            }

            Console.WriteLine();
            Console.WriteLine("The following devices are available on this machine:");
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine();

            for(int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"-----------Device {i}-----------");
                Console.WriteLine(devices[i].ToString());
            }

            Console.WriteLine("Usually device with local ip address is the correct one.");

            int deviceIndex = -1;

            do
            {
                Console.Write("Please select which device you would like to use: ");

                string input = Console.ReadLine();

                deviceIndex = int.TryParse(input, out int result) ? result : -1;

            } while (deviceIndex < 0 || deviceIndex >= devices.Count);

            _device = devices[deviceIndex];

            _logger.LogInformation("Chosen Device:");
            _logger.LogInformation(_device.ToString());

            _logger.LogInformation("Opening Device");
            _logger.LogInformation($"Using Timeout: {Timeout}");
            _device.Open(DeviceMode.Promiscuous, Timeout);

            return this;
        }

        public async Task Run()
        {
            running = true;

            Console.WriteLine();
            Console.WriteLine("-- Listening on {0}, hit 'ctrl-c' to stop...",
                _device.Name);

            while (running)
            {
                RawCapture capture = _device.GetNextPacket();

                if (capture == null)
                    continue;

                (string endpoint, string agent, string accept) = _packetExtractor.Extract(capture);

                if (endpoint == null)
                    continue;

                await _fileDownloader.DownloadFile(endpoint, agent, accept);
            }
        }

        public void Dispose()
        {
            _logger.LogInformation("Capture Statistics: ");
            _logger.LogInformation(_device.Statistics.ToString());

            _logger.LogInformation("Closing Device");
            _device.Close();

            Console.WriteLine("Hit Enter to exit...");
            Console.ReadLine();
        }

        public void Stop()
        {
            Console.WriteLine("-- Stopping capture");
            running = false;
        }
    }
}
