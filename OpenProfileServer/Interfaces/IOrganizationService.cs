using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.DTOs.Social;

namespace OpenProfileServer.Interfaces;

public interface IOrganizationService
{
    // === Core ===
    Task<ApiResponse<Guid>> CreateOrganizationAsync(Guid ownerId, CreateOrganizationRequestDto dto);
    Task<ApiResponse<IEnumerable<OrganizationDto>>> GetMyOrganizationsAsync(Guid userId);
    Task<ApiResponse<OrganizationDto>> GetOrganizationAsync(Guid userId, Guid orgId);
    Task<ApiResponse<MessageResponse>> DeleteOrganizationAsync(Guid ownerId, Guid orgId);
    
    /// <summary>
    /// Restores an organization that is in PendingDeletion status.
    /// </summary>
    Task<ApiResponse<MessageResponse>> RestoreOrganizationAsync(Guid ownerId, Guid orgId);
    
    Task<ApiResponse<FollowCountsDto>> GetOrgFollowCountsAsync(Guid userId, Guid orgId);
    
    Task<ApiResponse<IEnumerable<FollowerDto>>> GetOrgFollowersAsync(Guid userId, Guid orgId);
    Task<ApiResponse<IEnumerable<FollowerDto>>> GetOrgFollowingAsync(Guid userId, Guid orgId);

    // === Settings & Profile ===
    Task<ApiResponse<OrganizationSettingsDto>> GetOrgSettingsAsync(Guid userId, Guid orgId);
    Task<ApiResponse<MessageResponse>> UpdateOrgSettingsAsync(Guid userId, Guid orgId, UpdateOrganizationSettingsRequestDto dto);
    Task<ApiResponse<MessageResponse>> PatchOrgSettingsAsync(Guid userId, Guid orgId, UpdateOrganizationSettingsRequestDto dto);
    
    Task<ApiResponse<MessageResponse>> UpdateOrgProfileAsync(Guid userId, Guid orgId, UpdateProfileRequestDto dto);
    Task<ApiResponse<MessageResponse>> PatchOrgProfileAsync(Guid userId, Guid orgId, UpdateProfileRequestDto dto);

    // === Members (Self & Management by Owner) ===
    Task<ApiResponse<IEnumerable<OrganizationMemberDto>>> GetMembersAsync(Guid userId, Guid orgId);
    Task<ApiResponse<MemberRoleDto>> GetMyRoleAsync(Guid userId, Guid orgId);
    Task<ApiResponse<MessageResponse>> UpdateMyMemberDetailsAsync(Guid userId, Guid orgId, UpdateMemberRequestDto dto);
    
    Task<ApiResponse<MessageResponse>> RemoveMemberAsync(Guid requesterId, Guid orgId, Guid targetUserId);
    Task<ApiResponse<MessageResponse>> UpdateMemberRoleAsync(Guid requesterId, Guid orgId, Guid targetUserId, UpdateMemberRequestDto dto);
    Task<ApiResponse<MessageResponse>> LeaveOrganizationAsync(Guid userId, Guid orgId);
    
    // === Invitations ===
    Task<ApiResponse<IEnumerable<OrganizationInvitationDto>>> GetPendingInvitationsAsync(Guid requesterId, Guid orgId);
    Task<ApiResponse<MessageResponse>> InviteMemberAsync(Guid requesterId, Guid orgId, InviteMemberRequestDto dto);
    Task<ApiResponse<MessageResponse>> RevokeInvitationAsync(Guid requesterId, Guid orgId, Guid invitationId);
    
    Task<ApiResponse<IEnumerable<OrganizationInvitationDto>>> GetMyInvitationsAsync(Guid userId);
    Task<ApiResponse<MessageResponse>> RespondToInvitationAsync(Guid userId, Guid invitationId, bool accept);
}
