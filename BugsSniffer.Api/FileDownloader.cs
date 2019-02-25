using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BugsSniffer.Api
{
    public class FileDownloader
    {
        private readonly ILogger<FileDownloader> _logger;
        private readonly Dictionary<string, HttpClient> _clients;
        private readonly List<string> _downloads;

        private readonly object _lock = new object();

        public FileDownloader(ILoggerFactory factory)
        {
            _clients = new Dictionary<string, HttpClient>();
            _logger = factory.CreateLogger<FileDownloader>();
            _downloads = new List<string>();
        }

        public async Task DownloadFile(string host, string endpoint, string agent, string accept, string outputPath)
        {
            _logger.LogInformation($"Downloading {endpoint} on thread #{System.Threading.Thread.CurrentThread.ManagedThreadId}");
            _logger.LogInformation($"Attempting to download {outputPath}");

            if (IsDownloading(outputPath))
            {
                _logger.LogWarning($"Already downloading {outputPath}. Skipping file.");
                return;
            }
            else
            {
                AddFileToDownload(outputPath);
            }

            if (File.Exists(outputPath))
            {
                _logger.LogWarning($"{outputPath} exists. Skipping file.");
                RemoveFileFromDownload(outputPath);
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

                using (FileStream file = File.Open(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    _logger.LogInformation("Saving stream to file.");
                    await response.Content.CopyToAsync(file);
                    _logger.LogInformation($"Finished writing {outputPath}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception occured trying to download the file.");
            }

            RemoveFileFromDownload(outputPath);
        }

        private Task<HttpResponseMessage> SendAsync(string host, HttpRequestMessage request)
        {
            if (!_clients.Keys.Contains(host))
            {
                _clients.Add(host, new HttpClient
                {
                    BaseAddress = new Uri($"http://{host}"),
                    Timeout = TimeSpan.FromMinutes(10)
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