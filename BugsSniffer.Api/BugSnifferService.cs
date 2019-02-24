using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpPcap;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

            _fileDownloader = new FileDownloader(factory, config);
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

            string localIPAddress = LocalIPAddress()?.ToString() ?? string.Empty;

            Console.WriteLine($"Your Local IP Address is {localIPAddress}");

            if(devices.Count(device => device.ToString().Contains(localIPAddress)) == 1)
            {
                _device = devices.Single(device => device.ToString().Contains(localIPAddress));
                _logger.LogInformation("Automatically Selected Device:");
                _logger.LogInformation(_device.ToString());
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("The following devices are available on this machine:");
                Console.WriteLine("----------------------------------------------------");
                Console.WriteLine();

                for (int i = 0; i < devices.Count; i++)
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
            }

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

                (string host, string endpoint, string agent, string accept) = _packetExtractor.Extract(capture);

                if (endpoint == null)
                    continue;

                Parallel.Invoke(async() => await _fileDownloader.DownloadFile(host, endpoint, agent, accept));
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

        private IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}
