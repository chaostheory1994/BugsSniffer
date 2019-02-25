using BugsSniffer.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpPcap;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BugsSniffer.Api
{
    public class BugSnifferService : IDisposable
    {
        private readonly FileDownloader _fileDownloader;
        private readonly PacketExtractor _packetExtractor;
        private readonly ILogger<BugSnifferService> _logger;
        private readonly IConfiguration _config;
        private readonly MetadataResolver.MetadataResolver _metadataResolver;
        private readonly MetadataApplier.MetadataApplier _metadataApplier;
        private string outputDir => Environment.ExpandEnvironmentVariables(_config["outputDir"]);
        private ICaptureDevice _device;
        private bool running;

        public int Timeout => int.TryParse(_config["packetTimeout"], out int result) ? result : 0;

        public BugSnifferService(ILoggerFactory factory, IConfiguration config)
        {
            _logger = factory.CreateLogger<BugSnifferService>();
            _config = config;

            _logger.LogInformation($"Downloading Files to {outputDir}");

            _logger.LogInformation("Building HttpClient");

            _fileDownloader = new FileDownloader(factory);
            _packetExtractor = new PacketExtractor(factory);
            _metadataResolver = new MetadataResolver.MetadataResolver(factory);
            _metadataApplier = new MetadataApplier.MetadataApplier(factory);
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

                Parallel.Invoke(async() => await GetFile(host, endpoint, agent, accept));
            }
        }

        private async Task GetFile(string host, string endpoint, string agent, string accept)
        {
            try
            {
                string filename = Regex.Match(endpoint, @"[a-zA-Z0-9]*\.((mp4)|(flac))").Value;
                string extension = Path.GetExtension(filename);
                string id = filename.Split('.')[0];

                Metadata meta = await _metadataResolver.GetMetadata(filename);

                string saveFolder = outputDir;
                string fullPath = Path.Combine(saveFolder, filename);

                switch (meta)
                {
                    case Track track:
                        _logger.LogInformation($"Downloading song with metadata: {filename} => {track.FileName}{extension}");
                        saveFolder = Path.Combine(outputDir, track.Artist, track.Album);
                        fullPath = Path.Combine(saveFolder, $"{track.FileName}{extension}");

                        if (!Directory.Exists(saveFolder))
                        {
                            _logger.LogInformation($"Building folder for file: {saveFolder}");
                            Directory.CreateDirectory(saveFolder);
                        }

                        Uri albumArt = new Uri(track.AlbumArtUrl);
                        string albumArtExtension = Regex.Match(track.AlbumArtUrl, "[a-zA-Z0-0]*$").Value;
                        string albumArtPath = Path.Combine(saveFolder, $"{track.Album}.{albumArtExtension}");

                        Task fileDownload = _fileDownloader.DownloadFile(host, endpoint, agent, accept, fullPath);
                        Task albumArtDownload = _fileDownloader.DownloadFile(albumArt.Host, albumArt.PathAndQuery, agent, accept, albumArtPath);

                        Task.WaitAll(fileDownload, albumArtDownload);

                        await _metadataApplier.ApplyMetadata(saveFolder, $"{track.FileName}{extension}", track);

                        break;
                    case Movie movie:
                        saveFolder = Path.Combine(outputDir, movie.Artist);
                        fullPath = Path.Combine(saveFolder, $"{movie.FileName}{extension}");

                        if (!Directory.Exists(saveFolder))
                        {
                            _logger.LogInformation($"Building folder for file: {saveFolder}");
                            Directory.CreateDirectory(saveFolder);
                        }

                        await _fileDownloader.DownloadFile(host, endpoint, agent, accept, fullPath);
                        break;
                    default:
                        await _fileDownloader.DownloadFile(host, endpoint, agent, accept, fullPath);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "An error occured while trying to get the file.");
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
