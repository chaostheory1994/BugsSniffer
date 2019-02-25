using BugsSniffer.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BugsSniffer.Api.MetadataResolver.Resolvers
{
    public interface IMetadataResolver
    {
        List<string> SupportedFileTypes { get; }
        Task<Metadata> GetMetadata(string id);
    }
}