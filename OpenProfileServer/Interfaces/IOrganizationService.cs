using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Enums;

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

    // === Settings & Profile ===
    Task<ApiResponse<OrganizationSettingsDto>> GetOrgSettingsAsync(Guid userId, Guid orgId);
    Task<ApiResponse<MessageResponse>> UpdateOrgSettingsAsync(Guid userId, Guid orgId, UpdateOrganizationSettingsRequestDto dto);
    Task<ApiResponse<MessageResponse>> PatchOrgSettingsAsync(Guid userId, Guid orgId, UpdateOrganizationSettingsRequestDto dto);
    
    Task<ApiResponse<MessageResponse>> UpdateOrgProfileAsync(Guid userId, Guid orgId, UpdateProfileRequestDto dto);
    Task<ApiResponse<MessageResponse>> PatchOrgProfileAsync(Guid userId, Guid orgId, UpdateProfileRequestDto dto);

    // === Members ===
    Task<ApiResponse<IEnumerable<OrganizationMemberDto>>> GetMembersAsync(Guid userId, Guid orgId);
    Task<ApiResponse<MemberRoleDto>> GetMyRoleAsync(Guid userId, Guid orgId);
    Task<ApiResponse<MessageResponse>> UpdateMyMemberDetailsAsync(Guid userId, Guid orgId, UpdateMemberRequestDto dto);
    
    Task<ApiResponse<MessageResponse>> RemoveMemberAsync(Guid requesterId, Guid orgId, Guid targetUserId);
    Task<ApiResponse<MessageResponse>> UpdateMemberRoleAsync(Guid requesterId, Guid orgId, Guid targetUserId, UpdateMemberRequestDto dto);
    Task<ApiResponse<MessageResponse>> LeaveOrganizationAsync(Guid userId, Guid orgId);
    
    // === Invitations (Admin Side) ===
    Task<ApiResponse<IEnumerable<OrganizationInvitationDto>>> GetPendingInvitationsAsync(Guid requesterId, Guid orgId);
    Task<ApiResponse<MessageResponse>> InviteMemberAsync(Guid requesterId, Guid orgId, InviteMemberRequestDto dto);
    Task<ApiResponse<MessageResponse>> RevokeInvitationAsync(Guid requesterId, Guid orgId, Guid invitationId);
    
    // === Invitations (User Side) ===
    Task<ApiResponse<IEnumerable<OrganizationInvitationDto>>> GetMyInvitationsAsync(Guid userId);
    Task<ApiResponse<MessageResponse>> RespondToInvitationAsync(Guid userId, Guid invitationId, bool accept);
}
