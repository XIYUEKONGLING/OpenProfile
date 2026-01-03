using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Settings;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class OrganizationService : IOrganizationService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly INotificationService _notificationService;
    private static readonly Regex AccountNameRegex = new("^[a-zA-Z0-9_-]{3,64}$", RegexOptions.Compiled);

    public OrganizationService(
        ApplicationDbContext context, 
        IFusionCache cache,
        INotificationService notificationService)
    {
        _context = context;
        _cache = cache;
        _notificationService = notificationService;
    }

    // Helper: Check Permissions
    private async Task<(OrganizationMember? Member, OrganizationSettings? Settings)> GetMemberAndSettingsAsync(Guid userId, Guid orgId)
    {
        var member = await _context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        
        var settings = await _context.OrganizationSettings
            .FirstOrDefaultAsync(s => s.Id == orgId);

        return (member, settings);
    }

    public async Task<ApiResponse<Guid>> CreateOrganizationAsync(Guid ownerId, CreateOrganizationRequestDto dto)
    {
        // 1. Validate
        if (!AccountNameRegex.IsMatch(dto.AccountName))
            return ApiResponse<Guid>.Failure("Invalid account name.");

        if (await _context.Accounts.AnyAsync(a => a.AccountName == dto.AccountName))
            return ApiResponse<Guid>.Failure("Account name already taken.");

        // 2. Transaction
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
                Visibility = Visibility.Public,
                DefaultMemberVisibility = Visibility.Public
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
                .Include(m => m.Organization)
                .ThenInclude(p => p.Account)
                .Select(m => new OrganizationDto
                {
                    Id = m.OrganizationId,
                    AccountName = m.Organization.Account.AccountName,
                    DisplayName = m.Organization.DisplayName,
                    Avatar = new Models.DTOs.Core.AssetDto 
                    { 
                        Type = m.Organization.Avatar.Type, 
                        Value = m.Organization.Avatar.Value 
                    },
                    Status = m.Organization.Account.Status,
                    MyRole = m.Role
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        return ApiResponse<IEnumerable<OrganizationDto>>.Success(list ?? new List<OrganizationDto>());
    }

    public async Task<ApiResponse<MessageResponse>> DeleteOrganizationAsync(Guid ownerId, Guid orgId)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == ownerId);
        
        if (member == null || member.Role != MemberRole.Owner)
            return ApiResponse<MessageResponse>.Failure("Only the Owner can delete an organization.");

        var account = await _context.Accounts.FindAsync(orgId);
        if (account == null) return ApiResponse<MessageResponse>.Failure("Organization not found.");

        if (account.Status == AccountStatus.Banned)
            return ApiResponse<MessageResponse>.Failure("Cannot delete a banned organization.");

        // Soft delete / Cooling off
        account.Status = AccountStatus.PendingDeletion;
        await _context.SaveChangesAsync();
        
        // Invalidate public profile cache
        await _cache.RemoveAsync(CacheKeys.AccountProfile(orgId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Organization marked for deletion."));
    }

    public async Task<ApiResponse<OrganizationSettingsDto>> GetOrgSettingsAsync(Guid userId, Guid orgId)
    {
        var (member, settings) = await GetMemberAndSettingsAsync(userId, orgId);

        if (member == null || member.Role == MemberRole.Member || member.Role == MemberRole.Guest)
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

        if (member == null || member.Role != MemberRole.Owner) // Only Owner can change critical settings
            return ApiResponse<MessageResponse>.Failure("Only the Owner can update organization settings.");
        
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

    public async Task<ApiResponse<MessageResponse>> UpdateOrgProfileAsync(Guid userId, Guid orgId, UpdateProfileRequestDto dto)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        if (member == null || (member.Role != MemberRole.Owner && member.Role != MemberRole.Admin))
            return ApiResponse<MessageResponse>.Failure("Insufficient permissions.");

        var profile = await _context.OrganizationProfiles.FirstOrDefaultAsync(p => p.Id == orgId);
        if (profile == null) return ApiResponse<MessageResponse>.Failure("Profile not found.");

        // Update fields
        if (dto.DisplayName != null) profile.DisplayName = dto.DisplayName;
        if (dto.Description != null) profile.Description = dto.Description;
        if (dto.Content != null) profile.Content = dto.Content;
        if (dto.Location != null) profile.Location = dto.Location;
        if (dto.Website != null) profile.Website = dto.Website;
        if (dto.FoundedDate != null) profile.FoundedDate = dto.FoundedDate;

        if (dto.Avatar != null)
            profile.Avatar = new Asset { Type = dto.Avatar.Type, Value = dto.Avatar.Value, Tag = dto.Avatar.Tag };
        
        if (dto.Background != null)
            profile.Background = new Asset { Type = dto.Background.Type, Value = dto.Background.Value, Tag = dto.Background.Tag };

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.AccountProfile(orgId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Profile updated."));
    }

    public async Task<ApiResponse<IEnumerable<OrganizationMemberDto>>> GetMembersAsync(Guid userId, Guid orgId)
    {
        // Permission check: if Org is private, user must be a member.
        // For simplicity in this protected API, we assume if you are calling /api/orgs you want internal view.
        // If the user is NOT a member, they should use the Public Profile endpoint.
        
        var isMember = await _context.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.AccountId == userId);
        if (!isMember) return ApiResponse<IEnumerable<OrganizationMemberDto>>.Failure("You are not a member of this organization.");

        var members = await _context.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.OrganizationId == orgId)
            .Include(m => m.Account)
            .ThenInclude(a => a.Profile) // To get Avatar/Name
            .Select(m => new OrganizationMemberDto
            {
                AccountId = m.AccountId,
                AccountName = m.Account.AccountName,
                DisplayName = m.Account.Profile != null ? m.Account.Profile.DisplayName : "",
                Avatar = m.Account.Profile != null ? new Models.DTOs.Core.AssetDto 
                { 
                     Type = m.Account.Profile.Avatar.Type, 
                     Value = m.Account.Profile.Avatar.Value 
                } : new Models.DTOs.Core.AssetDto(),
                Role = m.Role,
                Title = m.Title,
                Visibility = m.Visibility,
                JoinedAt = m.JoinedAt
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<OrganizationMemberDto>>.Success(members);
    }

    public async Task<ApiResponse<MessageResponse>> InviteMemberAsync(Guid requesterId, Guid orgId, InviteMemberRequestDto dto)
    {
        var (requester, settings) = await GetMemberAndSettingsAsync(requesterId, orgId);
        
        if (requester == null) return ApiResponse<MessageResponse>.Failure("Not a member.");
        
        // Permission Check: Owner/Admin can always invite. Member can invite only if AllowMemberInvite is true.
        bool canInvite = requester.Role == MemberRole.Owner || requester.Role == MemberRole.Admin;
        if (!canInvite && requester.Role == MemberRole.Member && settings != null && settings.AllowMemberInvite)
        {
            canInvite = true;
        }

        if (!canInvite) return ApiResponse<MessageResponse>.Failure("Insufficient permissions to invite members.");

        // Resolve Invitee
        var invitee = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountName == dto.Identity || a.Emails.Any(e => e.Email == dto.Identity));
        if (invitee == null) return ApiResponse<MessageResponse>.Failure("User not found.");

        if (await _context.OrganizationMembers.AnyAsync(m => m.OrganizationId == orgId && m.AccountId == invitee.Id))
            return ApiResponse<MessageResponse>.Failure("User is already a member.");

        if (await _context.OrganizationInvitations.AnyAsync(i => i.OrganizationId == orgId && i.InviteeId == invitee.Id && i.Status == InvitationStatus.Pending))
            return ApiResponse<MessageResponse>.Failure("User already has a pending invitation.");

        // Create Invitation
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

        // Send Notification
        var orgName = (await _context.OrganizationProfiles.FindAsync(orgId))?.DisplayName ?? "An Organization";
        await _notificationService.CreateNotificationAsync(
            invitee.Id, 
            "Organization Invitation", 
            $"You have been invited to join {orgName}.", 
            NotificationType.Interaction, 
            $"{{\"invitationId\":\"{invitation.Id}\", \"orgId\":\"{orgId}\"}}"
        );

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Invitation sent."));
    }

    public async Task<ApiResponse<IEnumerable<OrganizationInvitationDto>>> GetPendingInvitationsAsync(Guid requesterId, Guid orgId)
    {
        var requester = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == requesterId);
        if (requester == null || (requester.Role != MemberRole.Owner && requester.Role != MemberRole.Admin))
            return ApiResponse<IEnumerable<OrganizationInvitationDto>>.Failure("Insufficient permissions.");
        
        var list = await _context.OrganizationInvitations
            .AsNoTracking()
            .Where(i => i.OrganizationId == orgId && i.Status == InvitationStatus.Pending)
            .Include(i => i.Invitee).ThenInclude(a => a.Profile) // Load profile for Avatar
            .Select(i => new OrganizationInvitationDto
            {
                Id = i.Id,
                OrganizationId = i.OrganizationId,
                // Org details usually known by context, but filling for consistency
                OrganizationName = i.Organization.DisplayName, 
                
                InviterId = i.InviterId,
                InviterName = i.Inviter.AccountName, // Or DisplayName
                
                InviteeId = i.InviteeId,
                InviteeName = i.Invitee.AccountName,
                InviteeAvatar = i.Invitee.Profile != null ? new Models.DTOs.Core.AssetDto 
                { 
                    Type = i.Invitee.Profile.Avatar.Type, 
                    Value = i.Invitee.Profile.Avatar.Value 
                } : new Models.DTOs.Core.AssetDto(),
                Role = i.Role,
                Title = i.Title,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
        return ApiResponse<IEnumerable<OrganizationInvitationDto>>.Success(list);
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
        var list = await _context.OrganizationInvitations
            .AsNoTracking()
            .Where(i => i.InviteeId == userId && i.Status == InvitationStatus.Pending)
            .Include(i => i.Organization)
            .Include(i => i.Inviter).ThenInclude(a => a.Profile)
            .Select(i => new OrganizationInvitationDto
            {
                Id = i.Id,
                OrganizationId = i.OrganizationId,
                OrganizationName = i.Organization.DisplayName,
                OrganizationAvatar = new Models.DTOs.Core.AssetDto 
                { 
                    Type = i.Organization.Avatar.Type, 
                    Value = i.Organization.Avatar.Value 
                },
                
                InviterId = i.InviterId,
                InviterName = i.Inviter.Profile != null ? i.Inviter.Profile.DisplayName : i.Inviter.AccountName,
                InviterAvatar = i.Inviter.Profile != null ? new Models.DTOs.Core.AssetDto
                {
                    Type = i.Inviter.Profile.Avatar.Type,
                    Value = i.Inviter.Profile.Avatar.Value
                } : new Models.DTOs.Core.AssetDto(),
                
                InviteeId = i.InviteeId,
                // Invitee is Me, no need to fill specific details usually
                
                Role = i.Role,
                Title = i.Title,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();
        
        return ApiResponse<IEnumerable<OrganizationInvitationDto>>.Success(list);
    }

    public async Task<ApiResponse<MessageResponse>> RespondToInvitationAsync(Guid userId, Guid invitationId, bool accept)
    {
        var invitation = await _context.OrganizationInvitations
            .Include(i => i.Organization) // Need org settings for default visibility
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
            
            // Notify Inviter
            await _notificationService.CreateNotificationAsync(
                invitation.InviterId,
                "Invitation Accepted",
                $"User accepted your invitation to join {invitation.Organization.DisplayName}.",
                NotificationType.Interaction
            );
            
            await _cache.RemoveAsync(CacheKeys.UserMemberships(userId));
            // If user joined with public visibility default, we should invalidate public cache too
            await _cache.RemoveAsync(CacheKeys.ProfileMemberships(userId));
            
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

    public async Task<ApiResponse<MessageResponse>> RemoveMemberAsync(Guid requesterId, Guid orgId, Guid targetUserId)
    {
        var requester = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == requesterId);
        var target = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == targetUserId);
        
        if (requester == null || target == null) return ApiResponse<MessageResponse>.Failure("Member not found.");

        // Rules:
        // Owner can kick anyone.
        // Admin can kick Member/Guest.
        // Cannot kick yourself (use Leave).
        
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
        
        if (requester == null || target == null) return ApiResponse<MessageResponse>.Failure("Member not found.");
        
        // Only Owner can change roles to Admin/Owner. 
        if (requester.Role != MemberRole.Owner) return ApiResponse<MessageResponse>.Failure("Only Owner can manage roles.");
        
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
            // Check if there are other members. If yes, must transfer ownership. If no, delete org.
            var count = await _context.OrganizationMembers.CountAsync(m => m.OrganizationId == orgId);
            if (count > 1) return ApiResponse<MessageResponse>.Failure("Owners cannot leave. Transfer ownership or dissolve the organization.");
        }

        _context.OrganizationMembers.Remove(member);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.UserMemberships(userId)); // My private list
        await _cache.RemoveAsync(CacheKeys.ProfileMemberships(userId)); // My public list

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Left organization."));
    }
}
