using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BugsSniffer.Api
{
    public class FileDownloader
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FileDownloader> _logger;
        private readonly Dictionary<string, HttpClient> _clients;
        private readonly List<string> _downloads;

        private readonly object _lock = new object();

        private string outputDir => Environment.ExpandEnvironmentVariables(_config["outputDir"]);

        public FileDownloader(ILoggerFactory factory, IConfiguration config)
        {
            _clients = new Dictionary<string, HttpClient>();
            _config = config;
            _logger = factory.CreateLogger<FileDownloader>();
            _logger.LogInformation($"Downloading Files to {outputDir}");
            _downloads = new List<string>();
        }

        public async Task DownloadFile(string host, string endpoint, string agent, string accept)
        {
            _logger.LogInformation($"Downloading {endpoint} on thread #{System.Threading.Thread.CurrentThread.ManagedThreadId}");
            string filename = Regex.Match(endpoint, @"[a-zA-Z0-9]*\.((mp4)|(flac))").Value;
            _logger.LogInformation($"Attempting to download {filename}");

            string fullPath = Path.Combine(outputDir, filename);

            if (IsDownloading(fullPath))
            {
                _logger.LogWarning($"Already downloading {fullPath}. Skipping file.");
                return;
            }
            else
            {
                AddFileToDownload(fullPath);
            }

            if (File.Exists(fullPath))
            {
                _logger.LogWarning($"{fullPath} exists. Skipping file.");
                RemoveFileFromDownload(fullPath);
                return;
            }

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, endpoint);
            message.Headers.Add("User-Agent", agent);
            message.Headers.Add("Accept", accept);

            try
            {
                HttpResponseMessage response = await SendAsync(host, message);
                response.EnsureSuccessStatusCode();
                await response.Content.LoadIntoBufferAsync();

                using (FileStream file = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    _logger.LogInformation("Saving stream to file.");
                    await response.Content.CopyToAsync(file);
                    _logger.LogInformation($"Finished writing {fullPath}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occured trying to download the file.");
            }

            RemoveFileFromDownload(fullPath);
        }

        private Task<HttpResponseMessage> SendAsync(string host, HttpRequestMessage request)
        {
            if (!_clients.Keys.Contains(host))
            {
                _clients.Add(host, new HttpClient
                {
                    BaseAddress = new Uri($"http://{host}")
                });
            }

            return _clients[host].SendAsync(request);
        }

        private bool IsDownloading(string fullPath)
        {
            lock (_lock)
            {
                return _downloads.Contains(fullPath);
            }
        }

        private void AddFileToDownload(string fullPath)
        {
            lock (_lock)
            {
                _downloads.Add(fullPath);
            }
        }

        private void RemoveFileFromDownload(string fullPath)
        {
            lock (_lock)
            {
                _downloads.Remove(fullPath);
            }
        }
    }
}