using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Profile.Details;

namespace OpenProfileServer.Interfaces;

public interface IProfileDetailService
{
    // Work Experience
    Task<ApiResponse<IEnumerable<WorkExperienceDto>>> GetWorkAsync(Guid profileId, bool publicOnly = false);
    Task<ApiResponse<MessageResponse>> AddWorkAsync(Guid accountId, UpdateWorkExperienceRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateWorkAsync(Guid accountId, Guid workId, UpdateWorkExperienceRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteWorkAsync(Guid accountId, Guid workId);

    // Education
    Task<ApiResponse<IEnumerable<EducationExperienceDto>>> GetEducationAsync(Guid profileId, bool publicOnly = false);
    Task<ApiResponse<MessageResponse>> AddEducationAsync(Guid accountId, UpdateEducationExperienceRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateEducationAsync(Guid accountId, Guid educationId, UpdateEducationExperienceRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteEducationAsync(Guid accountId, Guid educationId);

    // Projects
    Task<ApiResponse<IEnumerable<ProjectDto>>> GetProjectsAsync(Guid profileId, bool publicOnly = false);
    Task<ApiResponse<MessageResponse>> AddProjectAsync(Guid accountId, UpdateProjectRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateProjectAsync(Guid accountId, Guid projectId, UpdateProjectRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteProjectAsync(Guid accountId, Guid projectId);

    // Social Links
    Task<ApiResponse<IEnumerable<SocialLinkDto>>> GetSocialsAsync(Guid profileId); // Socials are generally public if the profile is public
    Task<ApiResponse<MessageResponse>> AddSocialAsync(Guid accountId, UpdateSocialLinkRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateSocialAsync(Guid accountId, Guid linkId, UpdateSocialLinkRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteSocialAsync(Guid accountId, Guid linkId);
    
    Task<ApiResponse<IEnumerable<PublicOrganizationMembershipDto>>> GetPublicMembershipsAsync(Guid profileId);
}
