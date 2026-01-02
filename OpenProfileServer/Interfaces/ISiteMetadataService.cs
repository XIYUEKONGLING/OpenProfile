using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.DTOs.Site;

namespace OpenProfileServer.Interfaces;

public interface ISiteMetadataService
{
    Task<SiteMetadata> GetMetadataAsync();
    
    /// <summary>
    /// Full update of site metadata.
    /// </summary>
    Task UpdateMetadataAsync(SiteMetadata metadata);

    /// <summary>
    /// Partial update of site metadata.
    /// </summary>
    Task PatchMetadataAsync(UpdateSiteMetadataRequestDto patchDto);
}