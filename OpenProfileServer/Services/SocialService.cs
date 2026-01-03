using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Social;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Enums;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class SocialService : ISocialService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly IProfileService _profileService;

    public SocialService(ApplicationDbContext context, IFusionCache cache, IProfileService profileService)
    {
        _context = context;
        _cache = cache;
        _profileService = profileService;
    }

    public async Task<ApiResponse<FollowStatusDto>> GetFollowStatusAsync(Guid observerId, Guid targetId)
    {
        var isFollowingKey = CacheKeys.SocialFollow(observerId, targetId);
        var isFollowedByKey = CacheKeys.SocialFollow(targetId, observerId);
        var isBlockingKey = CacheKeys.SocialBlock(observerId, targetId);
        var isBlockedByKey = CacheKeys.SocialBlock(targetId, observerId);

        var isFollowing = await _cache.GetOrSetAsync(isFollowingKey, async _ => 
            await _context.AccountFollowers.AnyAsync(f => f.FollowerId == observerId && f.FollowingId == targetId));
            
        var isFollowedBy = await _cache.GetOrSetAsync(isFollowedByKey, async _ => 
            await _context.AccountFollowers.AnyAsync(f => f.FollowerId == targetId && f.FollowingId == observerId));
            
        var isBlocking = await _cache.GetOrSetAsync(isBlockingKey, async _ => 
            await _context.AccountBlocks.AnyAsync(b => b.BlockerId == observerId && b.BlockedId == targetId));
            
        var isBlockedBy = await _cache.GetOrSetAsync(isBlockedByKey, async _ => 
            await _context.AccountBlocks.AnyAsync(b => b.BlockerId == targetId && b.BlockedId == observerId));

        return ApiResponse<FollowStatusDto>.Success(new FollowStatusDto
        {
            IsFollowing = isFollowing,
            IsFollowedBy = isFollowedBy,
            IsBlocking = isBlocking,
            IsBlockedBy = isBlockedBy
        });
    }

    public async Task<ApiResponse<MessageResponse>> FollowUserAsync(Guid followerId, Guid targetId)
    {
        if (followerId == targetId) 
            return ApiResponse<MessageResponse>.Failure("You cannot follow yourself.");

        // Check Target Existence AND Status
        var targetAccount = await _context.Accounts
            .AsNoTracking()
            .Select(a => new { a.Id, a.Status })
            .FirstOrDefaultAsync(a => a.Id == targetId);
            
        if (targetAccount == null) 
            return ApiResponse<MessageResponse>.Failure("Target user not found.");

        if (targetAccount.Status != AccountStatus.Active)
            return ApiResponse<MessageResponse>.Failure("Cannot follow this user (Account is not active).");

        // Check Blocks
        var isBlocked = await _context.AccountBlocks
            .AnyAsync(b => (b.BlockerId == targetId && b.BlockedId == followerId) || 
                           (b.BlockerId == followerId && b.BlockedId == targetId));

        if (isBlocked) return ApiResponse<MessageResponse>.Failure("You cannot follow this user.");

        var existing = await _context.AccountFollowers.FindAsync(followerId, targetId);
        if (existing != null) return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Already following."));

        _context.AccountFollowers.Add(new AccountFollower
        {
            FollowerId = followerId,
            FollowingId = targetId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.SocialFollow(followerId, targetId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Followed successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> UnfollowUserAsync(Guid followerId, Guid targetId)
    {
        var existing = await _context.AccountFollowers.FindAsync(followerId, targetId);
        if (existing == null) return ApiResponse<MessageResponse>.Failure("You are not following this user.");

        _context.AccountFollowers.Remove(existing);
        await _context.SaveChangesAsync();
        
        await _cache.RemoveAsync(CacheKeys.SocialFollow(followerId, targetId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Unfollowed successfully."));
    }

    public async Task<ApiResponse<MessageResponse>> BlockUserAsync(Guid blockerId, Guid targetId)
    {
        if (blockerId == targetId) return ApiResponse<MessageResponse>.Failure("You cannot block yourself.");

        var existingBlock = await _context.AccountBlocks.FindAsync(blockerId, targetId);
        if (existingBlock != null) return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Already blocked."));

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.AccountBlocks.Add(new AccountBlock
            {
                BlockerId = blockerId,
                BlockedId = targetId,
                CreatedAt = DateTime.UtcNow
            });

            // Auto-unfollow mutual
            var f1 = await _context.AccountFollowers.FindAsync(blockerId, targetId);
            if (f1 != null) _context.AccountFollowers.Remove(f1);

            var f2 = await _context.AccountFollowers.FindAsync(targetId, blockerId);
            if (f2 != null) _context.AccountFollowers.Remove(f2);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _cache.RemoveAsync(CacheKeys.SocialBlock(blockerId, targetId));
            await _cache.RemoveAsync(CacheKeys.SocialFollow(blockerId, targetId));
            await _cache.RemoveAsync(CacheKeys.SocialFollow(targetId, blockerId));

            return ApiResponse<MessageResponse>.Success(MessageResponse.Create("User blocked successfully."));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<MessageResponse>> UnblockUserAsync(Guid blockerId, Guid targetId)
    {
        var existing = await _context.AccountBlocks.FindAsync(blockerId, targetId);
        if (existing == null) return ApiResponse<MessageResponse>.Failure("User is not blocked.");

        _context.AccountBlocks.Remove(existing);
        await _context.SaveChangesAsync();
        
        await _cache.RemoveAsync(CacheKeys.SocialBlock(blockerId, targetId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("User unblocked."));
    }

    public async Task<ApiResponse<IEnumerable<FollowerDto>>> GetFollowersAsync(Guid accountId)
    {
        // 1. Check Privacy Settings
        var settings = await _context.AccountSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == accountId);

        if (settings != null && !settings.ShowFollowersList)
        {
            return ApiResponse<IEnumerable<FollowerDto>>.Success(new List<FollowerDto>());
        }

        // 2. Fetch Data with Filter (Status == Active)
        var followers = await _context.AccountFollowers
            .AsNoTracking()
            .Where(f => f.FollowingId == accountId)
            .Include(f => f.Follower)
            .ThenInclude(a => a.Profile)
            .Where(f => f.Follower.Status == AccountStatus.Active) 
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowerDto
            {
                AccountId = f.FollowerId,
                AccountName = f.Follower.AccountName,
                DisplayName = f.Follower.Profile != null ? f.Follower.Profile.DisplayName : "",
                Avatar = f.Follower.Profile != null ? new AssetDto
                {
                    Type = f.Follower.Profile.Avatar.Type,
                    Value = f.Follower.Profile.Avatar.Value,
                    Tag = f.Follower.Profile.Avatar.Tag
                } : new AssetDto(),
                Description = f.Follower.Profile != null ? f.Follower.Profile.Description : null
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<FollowerDto>>.Success(followers);
    }

    public async Task<ApiResponse<IEnumerable<FollowerDto>>> GetFollowingAsync(Guid accountId)
    {
        // 1. Check Privacy Settings
        var settings = await _context.AccountSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == accountId);

        if (settings != null && !settings.ShowFollowingList)
        {
            return ApiResponse<IEnumerable<FollowerDto>>.Success(new List<FollowerDto>());
        }

        // 2. Fetch Data with Filter (Status == Active)
        var following = await _context.AccountFollowers
            .AsNoTracking()
            .Where(f => f.FollowerId == accountId)
            .Include(f => f.Following)
            .ThenInclude(a => a.Profile)
            .Where(f => f.Following.Status == AccountStatus.Active)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowerDto
            {
                AccountId = f.FollowingId,
                AccountName = f.Following.AccountName,
                DisplayName = f.Following.Profile != null ? f.Following.Profile.DisplayName : "",
                Avatar = f.Following.Profile != null ? new AssetDto
                {
                    Type = f.Following.Profile.Avatar.Type,
                    Value = f.Following.Profile.Avatar.Value,
                    Tag = f.Following.Profile.Avatar.Tag
                } : new AssetDto(),
                Description = f.Following.Profile != null ? f.Following.Profile.Description : null
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<FollowerDto>>.Success(following);
    }

    public async Task<ApiResponse<IEnumerable<BlockDto>>> GetBlockedUsersAsync(Guid accountId)
    {
        // Block lists are private by definition, but this method is usually called by the owner via Admin/Me endpoints.
        // Assuming the Controller protects access to the owner.
        
        var blocks = await _context.AccountBlocks
            .AsNoTracking()
            .Where(b => b.BlockerId == accountId)
            .Include(b => b.Blocked)
            .ThenInclude(a => a.Profile)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BlockDto
            {
                AccountId = b.BlockedId,
                AccountName = b.Blocked.AccountName,
                DisplayName = b.Blocked.Profile != null ? b.Blocked.Profile.DisplayName : "",
                BlockedAt = b.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<BlockDto>>.Success(blocks);
    }
}
