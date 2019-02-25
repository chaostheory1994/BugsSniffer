using BugsSniffer.Api.MetadataResolver.Resolvers;
using BugsSniffer.Api.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BugsSniffer.Api.MetadataResolver
{
    public class MetadataResolver
    {
        private readonly List<IMetadataResolver> _resolvers;
        private readonly ILogger<MetadataResolver> _logger;
        private readonly List<string> _supportedFileTypes;

        public MetadataResolver(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<MetadataResolver>();

            HttpClient client = new HttpClient();

            _resolvers = new List<IMetadataResolver>
            {
                new SongMetadataResolver(client, factory),
                new MovieMetadataResolver(client, factory)
            };

            _supportedFileTypes = _resolvers.SelectMany(resolver => resolver.SupportedFileTypes).ToList();
        }

        public Task<Metadata> GetMetadata(string originalName)
        {
            string[] nameParts = originalName.Split('.');

            if(nameParts.Length != 2)
            {
                _logger.LogInformation("Could not seperate filename. No metadata being searched.");
                return Task.FromResult<Metadata>(null);
            }

            string extension = nameParts[1];
            if (!_supportedFileTypes.Contains(extension))
            {
                _logger.LogInformation($"Filetype {extension} is not supported for appling metadata.");
                return Task.FromResult<Metadata>(null);
            }
            else
            {
                return _resolvers.First(resolver => resolver.SupportedFileTypes.Contains(extension)).GetMetadata(nameParts[0]);
            }
        }
    }
}