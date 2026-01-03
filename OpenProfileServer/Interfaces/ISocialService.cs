using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Social;

namespace OpenProfileServer.Interfaces;

public interface ISocialService
{
    Task<ApiResponse<FollowStatusDto>> GetFollowStatusAsync(Guid observerId, Guid targetId);
    
    Task<ApiResponse<MessageResponse>> FollowUserAsync(Guid followerId, Guid targetId);
    Task<ApiResponse<MessageResponse>> UnfollowUserAsync(Guid followerId, Guid targetId);
    
    Task<ApiResponse<MessageResponse>> BlockUserAsync(Guid blockerId, Guid targetId);
    Task<ApiResponse<MessageResponse>> UnblockUserAsync(Guid blockerId, Guid targetId);
    
    Task<ApiResponse<IEnumerable<FollowerDto>>> GetFollowersAsync(Guid accountId, PaginationFilter filter);
    Task<ApiResponse<IEnumerable<FollowerDto>>> GetFollowingAsync(Guid accountId, PaginationFilter filter);
    Task<ApiResponse<IEnumerable<BlockDto>>> GetBlockedUsersAsync(Guid accountId);
}