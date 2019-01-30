using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BugsSniffer.Api
{
    public class FileDownloader
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _client;
        private readonly ILogger<FileDownloader> _logger;

        private string outputDir => Environment.ExpandEnvironmentVariables(_config["outputDir"]);

        public FileDownloader(ILoggerFactory factory, IConfiguration config, HttpClient client)
        {
            _config = config;
            _client = client;
            _logger = factory.CreateLogger<FileDownloader>();
            _logger.LogInformation($"Downloading Files to {outputDir}");
        }

        public async Task DownloadFile(string endpoint, string agent, string accept)
        {
            string filename = Regex.Match(endpoint, @"[a-zA-Z0-9]*\.flac").Value;
            _logger.LogInformation($"Attempting to download {filename}");

            string fullPath = Path.Combine(outputDir, filename);

            if (File.Exists(fullPath))
            {
                _logger.LogWarning($"{fullPath} exists. Skipping file.");
                return;
            }

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, endpoint);
            message.Headers.Add("User-Agent", agent);
            message.Headers.Add("Accept", accept);

            HttpResponseMessage response = await _client.SendAsync(message);
            response.EnsureSuccessStatusCode();
            await response.Content.LoadIntoBufferAsync();

            using (FileStream file = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                _logger.LogInformation("Saving stream to file.");
                await response.Content.CopyToAsync(file);
                _logger.LogInformation($"Finished writing {fullPath}");
            }
        }
    }
}

