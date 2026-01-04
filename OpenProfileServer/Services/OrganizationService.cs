using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Account;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.DTOs.Social;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;
using OpenProfileServer.Utilities;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly INotificationService _notificationService;
    private readonly ISystemSettingService _settingService;
    private readonly ISocialService _socialService;
    
    private static readonly Regex AccountNameRegex = new("^[a-zA-Z0-9_-]{3,64}$", RegexOptions.Compiled);

    public OrganizationService(
        ApplicationDbContext context, 
        IFusionCache cache,
        INotificationService notificationService,
        ISystemSettingService settingService,
        ISocialService socialService)
    {
        _context = context;
        _cache = cache;
        _notificationService = notificationService;
        _settingService = settingService;
        _socialService = socialService;
    }

    private async Task<string?> ValidateOrgAssetsAsync(UpdateProfileRequestDto dto)
    {
        int limit = await _settingService.GetIntAsync(SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        
        var vAvatar = AssetValidator.Validate(dto.Avatar, limit);
        if (!vAvatar.Valid) return vAvatar.Error;

        var vBackground = AssetValidator.Validate(dto.Background, limit);
        if (!vBackground.Valid) return vBackground.Error;

        return null;
    }

    private async Task<(OrganizationMember? Member, OrganizationSettings? Settings)> GetMemberAndSettingsAsync(Guid userId, Guid orgId)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        var settings = await _context.OrganizationSettings
            .FirstOrDefaultAsync(s => s.Id == orgId);
        return (member, settings);
    }
    
    public async Task<ApiResponse<IEnumerable<FollowerDto>>> GetOrgFollowersAsync(Guid userId, Guid orgId)
    {
        var isMember = await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == orgId && m.AccountId == userId);

        if (!isMember)
        {
            return ApiResponse<IEnumerable<FollowerDto>>.Failure("Insufficient permissions. You are not a member of this organization.");
        }

        return await _socialService.GetFollowersAsync(orgId, skipPrivacyCheck: true);
    }

    public async Task<ApiResponse<IEnumerable<FollowerDto>>> GetOrgFollowingAsync(Guid userId, Guid orgId)
    {
        var isMember = await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == orgId && m.AccountId == userId);

        if (!isMember)
        {
            return ApiResponse<IEnumerable<FollowerDto>>.Failure("Insufficient permissions. You are not a member of this organization.");
        }

        return await _socialService.GetFollowingAsync(orgId, skipPrivacyCheck: true);
    }

    public async Task<ApiResponse<Guid>> CreateOrganizationAsync(Guid ownerId, CreateOrganizationRequestDto dto)
    {
        if (!AccountNameRegex.IsMatch(dto.AccountName))
            return ApiResponse<Guid>.Failure("Invalid account name.");

        if (await _context.Accounts.AnyAsync(a => a.AccountName.ToLower() == dto.AccountName.ToLowerInvariant()))
            return ApiResponse<Guid>.Failure("Account name already taken.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var orgId = Guid.NewGuid();

            // Account
            var account = new Account
            {
                Id = orgId,
                AccountName = dto.AccountName,
                Type = AccountType.Organization,
                Role = AccountRole.User,
                Status = AccountStatus.Active
            };

            // Profile
            var profile = new OrganizationProfile
            {
                Id = orgId,
                Account = account,
                DisplayName = dto.DisplayName,
                Description = dto.Description,
                FoundedDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };

            // Settings
            var settings = new OrganizationSettings
            {
                Id = orgId,
                Account = account,
                Visibility = Visibility.Public
            };

            // Member (Owner)
            var member = new OrganizationMember
            {
                OrganizationId = orgId,
                AccountId = ownerId,
                Role = MemberRole.Owner,
                Title = "Founder"
            };

            _context.Accounts.Add(account);
            _context.OrganizationProfiles.Add(profile);
            _context.OrganizationSettings.Add(settings);
            _context.OrganizationMembers.Add(member);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _cache.RemoveAsync(CacheKeys.UserMemberships(ownerId));

            return ApiResponse<Guid>.Success(orgId, "Organization created.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ApiResponse<IEnumerable<OrganizationDto>>> GetMyOrganizationsAsync(Guid userId)
    {
        var cacheKey = CacheKeys.UserMemberships(userId);
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.AccountId == userId)
                .Include(m => m.Organization).ThenInclude(p => p.Account)
                .Select(m => new OrganizationDto
                {
                    Id = m.OrganizationId,
                    AccountName = m.Organization.Account.AccountName,
                    DisplayName = m.Organization.DisplayName,
                    Avatar = new Models.DTOs.Core.AssetDto { Type = m.Organization.Avatar.Type, Value = m.Organization.Avatar.Value },
                    Status = m.Organization.Account.Status,
                    MyRole = m.Role
                }).ToListAsync();
        }, tags: [cacheKey]);
        return ApiResponse<IEnumerable<OrganizationDto>>.Success(list ?? new List<OrganizationDto>());
    }

    public async Task<ApiResponse<OrganizationDto>> GetOrganizationAsync(Guid userId, Guid orgId)
    {
        var member = await _context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization).ThenInclude(o => o.Account)
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);

        if (member == null) return ApiResponse<OrganizationDto>.Failure("You are not a member of this organization.");

        return ApiResponse<OrganizationDto>.Success(new OrganizationDto
        {
            Id = member.OrganizationId,
            AccountName = member.Organization.Account.AccountName,
            DisplayName = member.Organization.DisplayName,
            Avatar = new Models.DTOs.Core.AssetDto { Type = member.Organization.Avatar.Type, Value = member.Organization.Avatar.Value },
            Status = member.Organization.Account.Status,
            MyRole = member.Role
        });
    }

    public async Task<ApiResponse<MessageResponse>> DeleteOrganizationAsync(Guid ownerId, Guid orgId)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == ownerId);
        if (member == null || member.Role != MemberRole.Owner)
            return ApiResponse<MessageResponse>.Failure("Only the Owner can delete an organization.");

        var account = await _context.Accounts.FindAsync(orgId);
        if (account == null) return ApiResponse<MessageResponse>.Failure("Organization not found.");
        if (account.Status == AccountStatus.Banned) return ApiResponse<MessageResponse>.Failure("Cannot delete a banned organization.");

        // Soft delete / Cooling off
        account.Status = AccountStatus.PendingDeletion;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountProfile(orgId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Organization marked for deletion."));
    }

    public async Task<ApiResponse<MessageResponse>> RestoreOrganizationAsync(Guid ownerId, Guid orgId)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == ownerId);
        if (member == null || member.Role != MemberRole.Owner)
            return ApiResponse<MessageResponse>.Failure("Only the Owner can restore an organization.");

        var account = await _context.Accounts.FindAsync(orgId);
        if (account == null) return ApiResponse<MessageResponse>.Failure("Organization not found.");
        if (account.Status != AccountStatus.PendingDeletion && account.Status != AccountStatus.Suspended)
            return ApiResponse<MessageResponse>.Failure("Organization is not in a state that requires restoration.");

        account.Status = AccountStatus.Active;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountProfile(orgId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Organization restored successfully."));
    }
    
    public async Task<ApiResponse<FollowCountsDto>> GetOrgFollowCountsAsync(Guid userId, Guid orgId)
    {
        var isMember = await _context.OrganizationMembers
            .AnyAsync(m => m.OrganizationId == orgId && m.AccountId == userId);

        if (!isMember)
        {
            return ApiResponse<FollowCountsDto>.Failure("Insufficient permissions. You are not a member of this organization.");
        }

        var followersCount = await _context.AccountFollowers
            .CountAsync(f => f.FollowingId == orgId);

        var followingCount = await _context.AccountFollowers
            .CountAsync(f => f.FollowerId == orgId);

        return ApiResponse<FollowCountsDto>.Success(new FollowCountsDto
        {
            FollowersCount = followersCount,
            FollowingCount = followingCount
        });
    }


    // === Settings ===

    public async Task<ApiResponse<OrganizationSettingsDto>> GetOrgSettingsAsync(Guid userId, Guid orgId)
    {
        var (member, settings) = await GetMemberAndSettingsAsync(userId, orgId);
        if (member == null || (member.Role != MemberRole.Owner && member.Role != MemberRole.Admin))
             return ApiResponse<OrganizationSettingsDto>.Failure("Insufficient permissions.");
        
        if (settings == null) return ApiResponse<OrganizationSettingsDto>.Failure("Settings not found.");

        return ApiResponse<OrganizationSettingsDto>.Success(new OrganizationSettingsDto
        {
            AllowFollowers = settings.AllowFollowers,
            ShowFollowingList = settings.ShowFollowingList,
            ShowFollowersList = settings.ShowFollowersList,
            Visibility = settings.Visibility,
            DefaultVisibility = settings.DefaultVisibility,
            DefaultMemberVisibility = settings.DefaultMemberVisibility,
            AllowMemberInvite = settings.AllowMemberInvite
        });
    }

    public async Task<ApiResponse<MessageResponse>> UpdateOrgSettingsAsync(Guid userId, Guid orgId, UpdateOrganizationSettingsRequestDto dto)
    {
        var (member, settings) = await GetMemberAndSettingsAsync(userId, orgId);
        if (member == null || member.Role != MemberRole.Owner)
            return ApiResponse<MessageResponse>.Failure("Only the Owner can update settings.");
        
        if (settings == null) return ApiResponse<MessageResponse>.Failure("Settings not found.");

        settings.AllowFollowers = dto.AllowFollowers ?? true;
        settings.ShowFollowingList = dto.ShowFollowingList ?? true;
        settings.ShowFollowersList = dto.ShowFollowersList ?? true;
        settings.Visibility = dto.Visibility ?? Visibility.Public;
        settings.DefaultVisibility = dto.DefaultVisibility ?? Visibility.Public;
        settings.DefaultMemberVisibility = dto.DefaultMemberVisibility ?? Visibility.Private;
        settings.AllowMemberInvite = dto.AllowMemberInvite ?? false;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountSettings(orgId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Settings updated."));
    }

    public async Task<ApiResponse<MessageResponse>> PatchOrgSettingsAsync(Guid userId, Guid orgId, UpdateOrganizationSettingsRequestDto dto)
    {
        var (member, settings) = await GetMemberAndSettingsAsync(userId, orgId);
        if (member == null || member.Role != MemberRole.Owner)
            return ApiResponse<MessageResponse>.Failure("Only the Owner can update settings.");
        
        if (settings == null) return ApiResponse<MessageResponse>.Failure("Settings not found.");

        if (dto.AllowFollowers.HasValue) settings.AllowFollowers = dto.AllowFollowers.Value;
        if (dto.ShowFollowingList.HasValue) settings.ShowFollowingList = dto.ShowFollowingList.Value;
        if (dto.ShowFollowersList.HasValue) settings.ShowFollowersList = dto.ShowFollowersList.Value;
        if (dto.Visibility.HasValue) settings.Visibility = dto.Visibility.Value;
        if (dto.DefaultVisibility.HasValue) settings.DefaultVisibility = dto.DefaultVisibility.Value;
        if (dto.DefaultMemberVisibility.HasValue) settings.DefaultMemberVisibility = dto.DefaultMemberVisibility.Value;
        if (dto.AllowMemberInvite.HasValue) settings.AllowMemberInvite = dto.AllowMemberInvite.Value;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountSettings(orgId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Settings updated."));
    }

    // === Profile ===

    public async Task<ApiResponse<MessageResponse>> UpdateOrgProfileAsync(Guid userId, Guid orgId, UpdateProfileRequestDto dto)
    {
        var assetError = await ValidateOrgAssetsAsync(dto);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        if (member == null || (member.Role != MemberRole.Owner && member.Role != MemberRole.Admin))
            return ApiResponse<MessageResponse>.Failure("Insufficient permissions.");

        var profile = await _context.OrganizationProfiles.Include(p => p.Account).FirstOrDefaultAsync(p => p.Id == orgId);
        if (profile == null) return ApiResponse<MessageResponse>.Failure("Profile not found.");

        profile.DisplayName = dto.DisplayName ?? profile.Account.AccountName;
        profile.Description = dto.Description;
        profile.Content = dto.Content;
        profile.Location = dto.Location;
        profile.Website = dto.Website;
        profile.FoundedDate = dto.FoundedDate;
        profile.Avatar = dto.Avatar != null ? new Asset { Type = dto.Avatar.Type, Value = dto.Avatar.Value, Tag = dto.Avatar.Tag } : new Asset();
        profile.Background = dto.Background != null ? new Asset { Type = dto.Background.Type, Value = dto.Background.Value, Tag = dto.Background.Tag } : new Asset();

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountProfile(orgId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Profile updated."));
    }

    public async Task<ApiResponse<MessageResponse>> PatchOrgProfileAsync(Guid userId, Guid orgId, UpdateProfileRequestDto dto)
    {
        var assetError = await ValidateOrgAssetsAsync(dto);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        if (member == null || (member.Role != MemberRole.Owner && member.Role != MemberRole.Admin))
            return ApiResponse<MessageResponse>.Failure("Insufficient permissions.");

        var profile = await _context.OrganizationProfiles.FirstOrDefaultAsync(p => p.Id == orgId);
        if (profile == null) return ApiResponse<MessageResponse>.Failure("Profile not found.");

        if (dto.DisplayName != null) profile.DisplayName = dto.DisplayName;
        if (dto.Description != null) profile.Description = dto.Description;
        if (dto.Content != null) profile.Content = dto.Content;
        if (dto.Location != null) profile.Location = dto.Location;
        if (dto.Website != null) profile.Website = dto.Website;
        if (dto.FoundedDate != null) profile.FoundedDate = dto.FoundedDate;
        if (dto.Avatar != null) profile.Avatar = new Asset { Type = dto.Avatar.Type, Value = dto.Avatar.Value, Tag = dto.Avatar.Tag };
        if (dto.Background != null) profile.Background = new Asset { Type = dto.Background.Type, Value = dto.Background.Value, Tag = dto.Background.Tag };

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountProfile(orgId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Profile updated."));
    }

    // === Members ===

    public async Task<ApiResponse<IEnumerable<OrganizationMemberDto>>> GetMembersAsync(Guid userId, Guid orgId)
    {
        var isMember = await _context.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        if (!isMember) return ApiResponse<IEnumerable<OrganizationMemberDto>>.Failure("You are not a member of this organization.");

        var members = await _context.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.OrganizationId == orgId)
            .Include(m => m.Account).ThenInclude(a => a.Profile)
            .Select(m => new OrganizationMemberDto
            {
                AccountId = m.AccountId,
                AccountName = m.Account.AccountName,
                DisplayName = m.Account.Profile != null ? m.Account.Profile.DisplayName : "",
                Avatar = m.Account.Profile != null ? new Models.DTOs.Core.AssetDto { Type = m.Account.Profile.Avatar.Type, Value = m.Account.Profile.Avatar.Value } : new Models.DTOs.Core.AssetDto(),
                Role = m.Role,
                Title = m.Title,
                Visibility = m.Visibility,
                JoinedAt = m.JoinedAt
            }).ToListAsync();

        return ApiResponse<IEnumerable<OrganizationMemberDto>>.Success(members);
    }

    public async Task<ApiResponse<MemberRoleDto>> GetMyRoleAsync(Guid userId, Guid orgId)
    {
        var member = await _context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);

        if (member == null) return ApiResponse<MemberRoleDto>.Failure("Not a member.");

        return ApiResponse<MemberRoleDto>.Success(new MemberRoleDto
        {
            OrganizationId = member.OrganizationId,
            OrganizationName = member.Organization.DisplayName,
            Role = member.Role,
            Title = member.Title,
            Visibility = member.Visibility
        });
    }

    public async Task<ApiResponse<MessageResponse>> UpdateMyMemberDetailsAsync(Guid userId, Guid orgId, UpdateMemberRequestDto dto)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        if (member == null) return ApiResponse<MessageResponse>.Failure("Not a member.");

        if (dto.Title != null) member.Title = dto.Title;
        if (dto.Visibility.HasValue) member.Visibility = dto.Visibility.Value;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileMemberships(userId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Member details updated."));
    }

    public async Task<ApiResponse<MessageResponse>> RemoveMemberAsync(Guid requesterId, Guid orgId, Guid targetUserId)
    {
        var requester = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == requesterId);
        var target = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == targetUserId);

        if (requester == null) return ApiResponse<MessageResponse>.Failure("Insufficient permissions.");
        if (target == null) return ApiResponse<MessageResponse>.Failure("Member not found.");
        if (requesterId == targetUserId) return ApiResponse<MessageResponse>.Failure("Use 'Leave' to remove yourself.");

        bool canKick = false;
        if (requester.Role == MemberRole.Owner) canKick = true;
        if (requester.Role == MemberRole.Admin && (target.Role == MemberRole.Member || target.Role == MemberRole.Guest)) canKick = true;

        if (!canKick) return ApiResponse<MessageResponse>.Failure("Insufficient permissions.");

        _context.OrganizationMembers.Remove(target);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.UserMemberships(targetUserId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Member removed."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateMemberRoleAsync(Guid requesterId, Guid orgId, Guid targetUserId, UpdateMemberRequestDto dto)
    {
        var requester = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == requesterId);
        var target = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == targetUserId);

        if (requester == null || requester.Role != MemberRole.Owner) return ApiResponse<MessageResponse>.Failure("Only Owner can manage roles.");
        if (target == null) return ApiResponse<MessageResponse>.Failure("Member not found.");
        
        if (requesterId == targetUserId && dto.Role.HasValue && dto.Role.Value != MemberRole.Owner)
            return ApiResponse<MessageResponse>.Failure("You cannot demote yourself. Transfer ownership first.");

        if (dto.Role.HasValue) target.Role = dto.Role.Value;
        if (dto.Title != null) target.Title = dto.Title;
        if (dto.Visibility.HasValue) target.Visibility = dto.Visibility.Value;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileMemberships(targetUserId));
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Member updated."));
    }

    public async Task<ApiResponse<MessageResponse>> LeaveOrganizationAsync(Guid userId, Guid orgId)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        if (member == null) return ApiResponse<MessageResponse>.Failure("Not a member.");

        if (member.Role == MemberRole.Owner)
        {
            // var count = await _context.OrganizationMembers.CountAsync(m => m.OrganizationId == orgId);
            // if (count > 1) return ApiResponse<MessageResponse>.Failure("Owners cannot leave. Transfer ownership or dissolve the organization.");
            var ownerCount = await _context.OrganizationMembers
                .CountAsync(m => m.OrganizationId == orgId && m.Role == MemberRole.Owner);
    
            if (ownerCount == 1) 
            {
                return ApiResponse<MessageResponse>.Failure("The last Owner cannot leave. Transfer ownership or dissolve the organization.");
            }
        }

        _context.OrganizationMembers.Remove(member);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.UserMemberships(userId));
        await _cache.RemoveAsync(CacheKeys.ProfileMemberships(userId));
        await _cache.RemoveAsync(CacheKeys.OrganizationMembers(orgId)); 
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Left organization."));
    }

    // === Invitations ===

    public async Task<ApiResponse<IEnumerable<OrganizationInvitationDto>>> GetPendingInvitationsAsync(Guid requesterId, Guid orgId)
    {
        var requester = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == requesterId);
        if (requester == null || (requester.Role != MemberRole.Owner && requester.Role != MemberRole.Admin))
            return ApiResponse<IEnumerable<OrganizationInvitationDto>>.Failure("Insufficient permissions.");
        
        var list = await _context.OrganizationInvitations.AsNoTracking()
            .Where(i => i.OrganizationId == orgId && i.Status == InvitationStatus.Pending)
            .Include(i => i.Invitee).ThenInclude(a => a.Profile)
            .Select(i => new OrganizationInvitationDto
            {
                Id = i.Id,
                OrganizationId = i.OrganizationId,
                OrganizationName = i.Organization.DisplayName, 
                InviterId = i.InviterId,
                InviterName = i.Inviter.AccountName,
                InviteeId = i.InviteeId,
                InviteeName = i.Invitee.AccountName,
                InviteeAvatar = i.Invitee.Profile != null ? new Models.DTOs.Core.AssetDto { Type = i.Invitee.Profile.Avatar.Type, Value = i.Invitee.Profile.Avatar.Value } : new Models.DTOs.Core.AssetDto(),
                Role = i.Role,
                Title = i.Title,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            }).ToListAsync();
        return ApiResponse<IEnumerable<OrganizationInvitationDto>>.Success(list);
    }

    public async Task<ApiResponse<MessageResponse>> InviteMemberAsync(Guid requesterId, Guid orgId, InviteMemberRequestDto dto)
    {
        var (requester, settings) = await GetMemberAndSettingsAsync(requesterId, orgId);
        if (requester == null) return ApiResponse<MessageResponse>.Failure("Not a member.");

        bool canInvite = false;
        if (requester.Role == MemberRole.Owner || requester.Role == MemberRole.Admin) canInvite = true;
        else if (requester.Role == MemberRole.Member && settings != null && settings.AllowMemberInvite) canInvite = true;

        if (!canInvite) return ApiResponse<MessageResponse>.Failure("Insufficient permissions.");

        var invitee = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountName.ToLower() == dto.Identity.ToLower() || a.Emails.Any(e => e.Email.ToLower() == dto.Identity.ToLower()));
        if (invitee == null) return ApiResponse<MessageResponse>.Failure("User not found.");

        if (await _context.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.AccountId == invitee.Id))
            return ApiResponse<MessageResponse>.Failure("User is already a member.");

        if (await _context.OrganizationInvitations.AnyAsync(i => i.OrganizationId == orgId && i.InviteeId == invitee.Id && i.Status == InvitationStatus.Pending))
            return ApiResponse<MessageResponse>.Failure("User already has a pending invitation.");

        var invitation = new OrganizationInvitation
        {
            OrganizationId = orgId,
            InviterId = requesterId,
            InviteeId = invitee.Id,
            Role = dto.Role,
            Title = dto.Title,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.OrganizationInvitations.Add(invitation);
        await _context.SaveChangesAsync();

        var orgName = (await _context.OrganizationProfiles.FindAsync(orgId))?.DisplayName ?? "An Organization";
        await _notificationService.CreateNotificationAsync(invitee.Id, "Organization Invitation", $"You have been invited to join {orgName}.", NotificationType.Interaction, $"{{\"invitationId\":\"{invitation.Id}\", \"orgId\":\"{orgId}\"}}");

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Invitation sent."));
    }

    public async Task<ApiResponse<MessageResponse>> RevokeInvitationAsync(Guid requesterId, Guid orgId, Guid invitationId)
    {
        var requester = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == requesterId);
        if (requester == null || (requester.Role != MemberRole.Owner && requester.Role != MemberRole.Admin))
             return ApiResponse<MessageResponse>.Failure("Insufficient permissions.");

        var invitation = await _context.OrganizationInvitations.FirstOrDefaultAsync(i => i.Id == invitationId && i.OrganizationId == orgId);
        if (invitation == null) return ApiResponse<MessageResponse>.Failure("Invitation not found.");

        _context.OrganizationInvitations.Remove(invitation);
        await _context.SaveChangesAsync();
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Invitation revoked."));
    }

    public async Task<ApiResponse<IEnumerable<OrganizationInvitationDto>>> GetMyInvitationsAsync(Guid userId)
    {
        var list = await _context.OrganizationInvitations.AsNoTracking()
            .Where(i => i.InviteeId == userId && i.Status == InvitationStatus.Pending)
            .Include(i => i.Organization)
            .Include(i => i.Inviter).ThenInclude(a => a.Profile)
            .Select(i => new OrganizationInvitationDto
            {
                Id = i.Id,
                OrganizationId = i.OrganizationId,
                OrganizationName = i.Organization.DisplayName,
                OrganizationAvatar = new Models.DTOs.Core.AssetDto { Type = i.Organization.Avatar.Type, Value = i.Organization.Avatar.Value },
                InviterId = i.InviterId,
                InviterName = i.Inviter.Profile != null ? i.Inviter.Profile.DisplayName : i.Inviter.AccountName,
                InviterAvatar = i.Inviter.Profile != null ? new Models.DTOs.Core.AssetDto { Type = i.Inviter.Profile.Avatar.Type, Value = i.Inviter.Profile.Avatar.Value } : new Models.DTOs.Core.AssetDto(),
                InviteeId = i.InviteeId,
                Role = i.Role,
                Title = i.Title,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            }).ToListAsync();
        return ApiResponse<IEnumerable<OrganizationInvitationDto>>.Success(list);
    }

    public async Task<ApiResponse<MessageResponse>> RespondToInvitationAsync(Guid userId, Guid invitationId, bool accept)
    {
        var invitation = await _context.OrganizationInvitations.Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.InviteeId == userId);
        
        if (invitation == null || invitation.Status != InvitationStatus.Pending)
            return ApiResponse<MessageResponse>.Failure("Invitation not found or no longer valid.");

        if (accept)
        {
            var settings = await _context.OrganizationSettings.FindAsync(invitation.OrganizationId);
            var member = new OrganizationMember
            {
                OrganizationId = invitation.OrganizationId,
                AccountId = userId,
                Role = invitation.Role,
                Title = invitation.Title,
                Visibility = settings?.DefaultMemberVisibility ?? Visibility.Private,
                JoinedAt = DateTime.UtcNow
            };
            
            invitation.Status = InvitationStatus.Accepted;
            invitation.RespondedAt = DateTime.UtcNow;
            
            _context.OrganizationMembers.Add(member);
            await _context.SaveChangesAsync();
            
            await _notificationService.CreateNotificationAsync(invitation.InviterId, "Invitation Accepted", $"User accepted your invitation to join {invitation.Organization.DisplayName}.", NotificationType.Interaction);
            await _cache.RemoveAsync(CacheKeys.UserMemberships(userId));
            
            return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Invitation accepted."));
        }
        else
        {
            invitation.Status = InvitationStatus.Declined;
            invitation.RespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Invitation declined."));
        }
    }
}
