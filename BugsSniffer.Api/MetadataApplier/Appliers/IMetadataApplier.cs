using BugsSniffer.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BugsSniffer.Api.MetadataApplier.Appliers
{
    public interface IMetadataApplier
    {
        Task ApplyMetadata(string filePath, string filename, Track metadata);
        List<string> SupportedFileTypes { get; }
    }
}
