using BugsSniffer.Api.MetadataApplier.Appliers;
using BugsSniffer.Api.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BugsSniffer.Api.MetadataApplier
{
    public class MetadataApplier : IMetadataApplier
    {
        private readonly ILogger<MetadataApplier> _logger;
        private readonly List<IMetadataApplier> _appliers;

        public MetadataApplier(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<MetadataApplier>(); ;

            _appliers = new List<IMetadataApplier>
            {
                new FlacMetadataApplier(factory)
            };

            SupportedFileTypes = _appliers.SelectMany(applier => applier.SupportedFileTypes).ToList();
        }

        public List<string> SupportedFileTypes { get; }

        public Task ApplyMetadata(string filePath, string filename, Track metadata)
        {
            string extension = Path.GetExtension(filename).Replace(".", "");
            if (!SupportedFileTypes.Contains(extension))
            {
                _logger.LogInformation($"Filetype {extension} is not supported for appling metadata.");
                return Task.CompletedTask;
            }
            else
            {
                return _appliers.First(applier => applier.SupportedFileTypes.Contains(extension)).ApplyMetadata(filePath, filename, metadata);
            }
        }
    }
}