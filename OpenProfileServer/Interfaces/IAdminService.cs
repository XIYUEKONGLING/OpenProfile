using OpenProfileServer.Models.DTOs.Admin;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;

namespace OpenProfileServer.Interfaces;

public interface IAdminService
{
    /// <summary>
    /// Lists users with advanced filtering and pagination.
    /// </summary>
    Task<ApiResponse<PagedResponse<UserAdminDto>>> GetUsersAsync(PaginationFilter pagination, UserFilterDto filter);

    /// <summary>
    /// Forcefully creates a user/org (bypassing registration toggles).
    /// </summary>
    Task<ApiResponse<UserAdminDto>> CreateUserAsync(Guid adminId, CreateUserRequestDto dto);

    /// <summary>
    /// Updates account status (e.g., Ban, Suspend).
    /// </summary>
    Task<ApiResponse<MessageResponse>> UpdateUserStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequestDto dto);

    /// <summary>
    /// Promotes or Demotes a user (e.g., User <-> Admin).
    /// </summary>
    Task<ApiResponse<MessageResponse>> UpdateUserRoleAsync(Guid adminId, Guid targetUserId, UpdateUserRoleRequestDto dto);

    /// <summary>
    /// Physically deletes a user and all related data from the database.
    /// This is irreversible.
    /// </summary>
    Task<ApiResponse<MessageResponse>> DeleteUserAsync(Guid adminId, Guid targetUserId);
    
    // === Organization Management ===
    Task<ApiResponse<IEnumerable<OrganizationMemberDto>>> AdminGetOrgMembersAsync(Guid orgId);
    Task<ApiResponse<MessageResponse>> AdminAddMemberAsync(Guid orgId, Guid targetUserId, InviteMemberRequestDto dto);
    Task<ApiResponse<MessageResponse>> AdminUpdateMemberAsync(Guid orgId, Guid targetUserId, UpdateMemberRequestDto dto);
    Task<ApiResponse<MessageResponse>> AdminKickMemberAsync(Guid orgId, Guid targetUserId);
}