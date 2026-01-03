using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Admin;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Utilities;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly IAuthService _authService; // Used to revoke sessions
    
    private static readonly Regex AccountNameRegex = new("^[a-zA-Z0-9_-]{3,64}$", RegexOptions.Compiled);

    public AdminService(ApplicationDbContext context, IFusionCache cache, IAuthService authService)
    {
        _context = context;
        _cache = cache;
        _authService = authService;
    }

    public async Task<ApiResponse<PagedResponse<UserAdminDto>>> GetUsersAsync(PaginationFilter pagination, UserFilterDto filter)
    {
        var query = _context.Accounts
            .AsNoTracking()
            .Include(a => a.Emails)
            .AsQueryable();

        // 1. Apply Filtering
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(a => 
                a.AccountName.ToLower().Contains(term) || 
                a.Emails.Any(e => e.Email.ToLower().Contains(term)));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(a => a.Status == filter.Status.Value);
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(a => a.Role == filter.Role.Value);
        }

        // 2. Pagination Counts
        var totalRecords = await query.CountAsync();

        // 3. Fetch Data
        var users = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(a => new UserAdminDto
            {
                Id = a.Id,
                AccountName = a.AccountName,
                // Select primary email, fallback to any email, fallback to empty
                Email = a.Emails.FirstOrDefault(e => e.IsPrimary) != null 
                    ? a.Emails.First(e => e.IsPrimary).Email 
                    : (a.Emails.FirstOrDefault() != null ? a.Emails.First().Email : string.Empty),
                Type = a.Type,
                Role = a.Role,
                Status = a.Status,
                CreatedAt = a.CreatedAt,
                LastLogin = a.LastLogin
            })
            .ToListAsync();

        return ApiResponse<PagedResponse<UserAdminDto>>.Success(
            new PagedResponse<UserAdminDto>(users, pagination.PageNumber, pagination.PageSize, totalRecords));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateUserStatusAsync(Guid adminId, Guid targetUserId, UpdateUserStatusRequestDto dto)
    {
        var target = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == targetUserId);
        if (target == null) return ApiResponse<MessageResponse>.Failure("User not found.");

        // Protection: Cannot ban Root
        if (target.Role == AccountRole.Root)
            return ApiResponse<MessageResponse>.Failure("Cannot change status of Root account.");

        // Protection: Cannot ban yourself
        if (target.Id == adminId)
            return ApiResponse<MessageResponse>.Failure("Cannot change your own status.");

        target.Status = dto.Status;
        await _context.SaveChangesAsync();

        // Security: If banning/suspending, revoke all sessions immediately
        if (dto.Status == AccountStatus.Banned || dto.Status == AccountStatus.Suspended)
        {
            await _authService.LogoutAllDevicesAsync(targetUserId);
        }

        // Invalidate caches
        await _cache.RemoveAsync(CacheKeys.AccountProfile(targetUserId));
        await _cache.RemoveAsync(CacheKeys.AccountPermissions(targetUserId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create($"User status updated to {dto.Status}."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateUserRoleAsync(Guid adminId, Guid targetUserId, UpdateUserRoleRequestDto dto)
    {
        // Must be Root to change roles (according to API spec mostly, or strict Admin policy)
        // We will check the caller's role in the Controller via [Authorize], but double check here logic if needed.
        
        var target = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == targetUserId);
        if (target == null) return ApiResponse<MessageResponse>.Failure("User not found.");

        // Protection: Cannot modify Root
        if (target.Role == AccountRole.Root)
            return ApiResponse<MessageResponse>.Failure("Cannot modify Root account role.");

        if (dto.Role == AccountRole.Root)
            return ApiResponse<MessageResponse>.Failure("Cannot promote to Root via API.");

        target.Role = dto.Role;
        await _context.SaveChangesAsync();

        // Security: Role change requires token refresh to take effect (Claims are in JWT)
        // We force logout to ensure they get new claims immediately
        await _authService.LogoutAllDevicesAsync(targetUserId);
        await _cache.RemoveAsync(CacheKeys.AccountPermissions(targetUserId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create($"User role updated to {dto.Role}."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteUserAsync(Guid adminId, Guid targetUserId)
    {
        var target = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == targetUserId);
        if (target == null) return ApiResponse<MessageResponse>.Failure("User not found.");

        if (target.Role == AccountRole.Root)
            return ApiResponse<MessageResponse>.Failure("Cannot delete Root account.");

        if (target.Id == adminId)
            return ApiResponse<MessageResponse>.Failure("Cannot delete yourself.");

        // Physical Delete
        // EF Core Cascade Delete is configured in ApplicationDbContext for most relations.
        // Account is the principal entity.
        
        _context.Accounts.Remove(target);
        await _context.SaveChangesAsync();

        // Clean Cache
        await _cache.RemoveAsync(CacheKeys.AccountProfile(targetUserId));
        await _cache.RemoveAsync(CacheKeys.AccountSettings(targetUserId));
        await _cache.RemoveAsync(CacheKeys.AccountPermissions(targetUserId));
        await _cache.RemoveAsync(CacheKeys.AccountNameMapping(target.AccountName));
        
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("User permanently deleted."));
    }

    public async Task<ApiResponse<UserAdminDto>> CreateUserAsync(Guid adminId, CreateUserRequestDto dto)
    {
        // 1. Validation
        if (!AccountNameRegex.IsMatch(dto.AccountName))
            return ApiResponse<UserAdminDto>.Failure("Invalid account name format.");

        var accountNameLower = dto.AccountName.ToLowerInvariant();
        if (await _context.Accounts.AnyAsync(a => a.AccountName.ToLower() == accountNameLower))
            return ApiResponse<UserAdminDto>.Failure("Account name already taken.");

        if (await _context.AccountEmails.AnyAsync(e => e.Email.ToLower() == dto.Email.ToLower()))
            return ApiResponse<UserAdminDto>.Failure("Email already in use.");

        // Prevent creating Root via API
        if (dto.Role == AccountRole.Root)
            return ApiResponse<UserAdminDto>.Failure("Cannot create Root account via API.");

        // 2. Transaction
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var accountId = Guid.NewGuid();
            var (hash, salt) = CryptographyProvider.CreateHash(dto.Password);

            // Base Account
            var account = new Account
            {
                Id = accountId,
                AccountName = dto.AccountName,
                Type = dto.Type,
                Role = dto.Role,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                LastLogin = DateTime.UtcNow
            };

            // Credential
            var credential = new AccountCredential
            {
                AccountId = accountId,
                PasswordHash = hash,
                PasswordSalt = salt,
                UpdatedAt = DateTime.UtcNow
            };
            
            // Email (Auto-Verified)
            var email = new AccountEmail
            {
                AccountId = accountId,
                Email = dto.Email,
                IsPrimary = true,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Accounts.Add(account);
            _context.AccountCredentials.Add(credential);
            _context.AccountEmails.Add(email);

            // Polymorphic Logic
            if (dto.Type == AccountType.Personal)
            {
                var profile = new PersonalProfile
                {
                    Id = accountId,
                    Account = account,
                    DisplayName = dto.DisplayName ?? dto.AccountName,
                    Description = "Account created by Administrator."
                };
                
                var settings = new PersonalSettings
                {
                    Id = accountId,
                    Account = account,
                    Visibility = Visibility.Public
                };
                
                var security = new AccountSecurity { AccountId = accountId };
                
                _context.PersonalProfiles.Add(profile);
                _context.PersonalSettings.Add(settings);
                _context.AccountSecurities.Add(security);
            }
            else if (dto.Type == AccountType.Organization)
            {
                var profile = new OrganizationProfile
                {
                    Id = accountId,
                    Account = account,
                    DisplayName = dto.DisplayName ?? dto.AccountName,
                    Description = "Organization created by Administrator.",
                    FoundedDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                
                var settings = new OrganizationSettings
                {
                    Id = accountId,
                    Account = account,
                    Visibility = Visibility.Public
                };

                _context.OrganizationProfiles.Add(profile);
                _context.OrganizationSettings.Add(settings);
                // _context.OrganizationMembers.Add(member);
                // Create an empty organization. Admin can manage members later via override.
            }
            else
            {
                return ApiResponse<UserAdminDto>.Failure("Unsupported account type for creation.");
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ApiResponse<UserAdminDto>.Success(new UserAdminDto
            {
                Id = accountId,
                AccountName = account.AccountName,
                Email = dto.Email,
                Type = account.Type,
                Role = account.Role,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                LastLogin = account.LastLogin
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // ==========================================
    // Admin: Organization Management
    // ==========================================

    public async Task<ApiResponse<IEnumerable<OrganizationMemberDto>>> AdminGetOrgMembersAsync(Guid orgId)
    {
        // Admins can see members even if org is private
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
            })
            .ToListAsync();

        return ApiResponse<IEnumerable<OrganizationMemberDto>>.Success(members);
    }

    public async Task<ApiResponse<MessageResponse>> AdminAddMemberAsync(Guid orgId, Guid targetUserId, InviteMemberRequestDto dto)
    {
        // 1. Validate Existance
        var orgExists = await _context.OrganizationProfiles.AnyAsync(o => o.Id == orgId);
        if (!orgExists) return ApiResponse<MessageResponse>.Failure("Organization not found.");

        var userExists = await _context.Accounts.AnyAsync(a => a.Id == targetUserId);
        if (!userExists) return ApiResponse<MessageResponse>.Failure("User not found.");

        // 2. Check if already member
        var existing = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == targetUserId);
        if (existing != null) return ApiResponse<MessageResponse>.Failure("User is already a member.");

        // 3. Force Add (Bypass Invite)
        var settings = await _context.OrganizationSettings.FindAsync(orgId);
        
        var member = new OrganizationMember
        {
            OrganizationId = orgId,
            AccountId = targetUserId,
            Role = dto.Role,
            Title = dto.Title,
            Visibility = settings?.DefaultMemberVisibility ?? Visibility.Private,
            JoinedAt = DateTime.UtcNow
        };

        _context.OrganizationMembers.Add(member);
        
        // Clear pending invitations if any
        var invitations = await _context.OrganizationInvitations
            .Where(i => i.OrganizationId == orgId && i.InviteeId == targetUserId && i.Status == InvitationStatus.Pending)
            .ToListAsync();
        _context.OrganizationInvitations.RemoveRange(invitations);

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.UserMemberships(targetUserId));
        
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Member added successfully (Admin Override)."));
    }

    public async Task<ApiResponse<MessageResponse>> AdminUpdateMemberAsync(Guid orgId, Guid targetUserId, UpdateMemberRequestDto dto)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == targetUserId);
        if (member == null) return ApiResponse<MessageResponse>.Failure("Member not found.");

        // Admin has no restrictions on role changes (can promote to Owner)
        if (dto.Role.HasValue) member.Role = dto.Role.Value;
        if (dto.Title != null) member.Title = dto.Title;
        if (dto.Visibility.HasValue) member.Visibility = dto.Visibility.Value;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileMemberships(targetUserId));
        
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Member updated successfully (Admin Override)."));
    }

    public async Task<ApiResponse<MessageResponse>> AdminKickMemberAsync(Guid orgId, Guid targetUserId)
    {
        var member = await _context.OrganizationMembers.FirstOrDefaultAsync(m => m.OrganizationId == orgId && m.AccountId == targetUserId);
        if (member == null) return ApiResponse<MessageResponse>.Failure("Member not found.");

        // Safety check: Prevent kicking the SOLE owner unless organization is being deleted?
        // User requirement: "Admin operations should not be hindered".
        // However, leaving an org with NO owners breaks business logic. 
        // We will allow it but maybe logic elsewhere needs to handle orphan orgs. 
        // For now, adhere to "No Hindrance".

        _context.OrganizationMembers.Remove(member);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.UserMemberships(targetUserId));
        
        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Member kicked successfully (Admin Override)."));
    }

}
