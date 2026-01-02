using OpenProfileServer.Models.Entities;

namespace OpenProfileServer.Interfaces;

public interface ISiteMetadataService
{
    Task<SiteMetadata> GetMetadataAsync();
    Task UpdateMetadataAsync(SiteMetadata metadata);
}