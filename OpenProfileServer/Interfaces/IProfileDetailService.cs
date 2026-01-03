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
    Task<ApiResponse<IEnumerable<SocialLinkDto>>> GetSocialsAsync(Guid profileId);
    Task<ApiResponse<MessageResponse>> AddSocialAsync(Guid accountId, UpdateSocialLinkRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateSocialAsync(Guid accountId, Guid linkId, UpdateSocialLinkRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteSocialAsync(Guid accountId, Guid linkId);
    
    Task<ApiResponse<IEnumerable<PublicOrganizationMembershipDto>>> GetPublicMembershipsAsync(Guid profileId);

    // Certificates
    Task<ApiResponse<IEnumerable<CertificateDto>>> GetCertificatesAsync(Guid profileId, bool publicOnly = false);
    Task<ApiResponse<MessageResponse>> AddCertificateAsync(Guid accountId, UpdateCertificateRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateCertificateAsync(Guid accountId, Guid certId, UpdateCertificateRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteCertificateAsync(Guid accountId, Guid certId);

    // Sponsorships
    Task<ApiResponse<IEnumerable<SponsorshipItemDto>>> GetSponsorshipsAsync(Guid profileId, bool publicOnly = false);
    Task<ApiResponse<MessageResponse>> AddSponsorshipAsync(Guid accountId, UpdateSponsorshipItemRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateSponsorshipAsync(Guid accountId, Guid itemId, UpdateSponsorshipItemRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteSponsorshipAsync(Guid accountId, Guid itemId);

    // Gallery
    Task<ApiResponse<IEnumerable<GalleryItemDto>>> GetGalleryAsync(Guid profileId, bool publicOnly = false);
    Task<ApiResponse<MessageResponse>> AddGalleryItemAsync(Guid accountId, UpdateGalleryItemRequestDto dto);
    Task<ApiResponse<MessageResponse>> UpdateGalleryItemAsync(Guid accountId, Guid itemId, UpdateGalleryItemRequestDto dto);
    Task<ApiResponse<MessageResponse>> DeleteGalleryItemAsync(Guid accountId, Guid itemId);
}
