using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Profile;

namespace OpenProfileServer.Interfaces;

public interface IProfileService
{
    /// <summary>
    /// Resolves a string identifier (username or @uuid) to an AccountId.
    /// </summary>
    Task<Guid?> ResolveIdAsync(string identifier);

    /// <summary>
    /// Gets the public profile for a user. Content varies based on status.
    /// </summary>
    Task<ApiResponse<ProfileDto>> GetProfileAsync(string identifier);
}