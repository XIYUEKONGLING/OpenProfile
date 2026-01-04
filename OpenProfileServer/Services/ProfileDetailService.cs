using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Organization;
using OpenProfileServer.Models.DTOs.Profile;
using OpenProfileServer.Models.DTOs.Profile.Details;
using OpenProfileServer.Models.Entities.Details;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;
using OpenProfileServer.Utilities;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class ProfileDetailService : IProfileDetailService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;
    private readonly ISystemSettingService _settingService;

    public ProfileDetailService(ApplicationDbContext context, IFusionCache cache, ISystemSettingService settingService)
    {
        _context = context;
        _cache = cache;
        _settingService = settingService;
    }

    private async Task<string?> ValidateAssetAsync(AssetDto? asset, string limitKey, int defaultLimit)
    {
        int limit = await _settingService.GetIntAsync(limitKey, defaultLimit);
        var result = AssetValidator.Validate(asset, limit);
        return result.Valid ? null : result.Error;
    }
    
    public async Task<ApiResponse<IEnumerable<WorkExperienceDto>>> GetWorkAsync(Guid profileId, bool publicOnly = false)
    {
        var cacheKey = CacheKeys.ProfileWork(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.WorkExperiences
                .AsNoTracking()
                .Where(w => w.PersonalProfileId == profileId)
                .OrderByDescending(w => w.StartDate)
                .Select(w => new WorkExperienceDto
                {
                    Id = w.Id,
                    CompanyName = w.CompanyName,
                    Position = w.Position,
                    StartDate = w.StartDate,
                    EndDate = w.EndDate,
                    Description = w.Description,
                    Logo = new AssetDto { Type = w.Logo.Type, Value = w.Logo.Value, Tag = w.Logo.Tag }
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        return ApiResponse<IEnumerable<WorkExperienceDto>>.Success(list ?? new List<WorkExperienceDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddWorkAsync(Guid accountId, UpdateWorkExperienceRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Logo, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var isPersonal = await _context.PersonalProfiles.AnyAsync(p => p.Id == accountId);
        if (!isPersonal) return ApiResponse<MessageResponse>.Failure("Operation valid only for personal accounts.");

        var entity = new WorkExperience
        {
            PersonalProfileId = accountId,
            CompanyName = dto.CompanyName,
            Position = dto.Position ?? string.Empty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Description = dto.Description,
            Logo = dto.Logo != null ? new Asset { Type = dto.Logo.Type, Value = dto.Logo.Value, Tag = dto.Logo.Tag } : new Asset()
        };

        _context.WorkExperiences.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileWork(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Work experience added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateWorkAsync(Guid accountId, Guid workId, UpdateWorkExperienceRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Logo, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var entity = await _context.WorkExperiences.FirstOrDefaultAsync(w => w.Id == workId && w.PersonalProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        entity.CompanyName = dto.CompanyName;
        if (dto.Position != null) entity.Position = dto.Position;
        if (dto.StartDate != null) entity.StartDate = dto.StartDate;
        if (dto.EndDate != null) entity.EndDate = dto.EndDate;
        if (dto.Description != null) entity.Description = dto.Description;
        
        if (dto.Logo != null)
        {
            entity.Logo = new Asset { Type = dto.Logo.Type, Value = dto.Logo.Value, Tag = dto.Logo.Tag };
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileWork(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Work experience updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteWorkAsync(Guid accountId, Guid workId)
    {
        var entity = await _context.WorkExperiences.FirstOrDefaultAsync(w => w.Id == workId && w.PersonalProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.WorkExperiences.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileWork(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Work experience deleted."));
    }

    public async Task<ApiResponse<IEnumerable<EducationExperienceDto>>> GetEducationAsync(Guid profileId, bool publicOnly = false)
    {
        var cacheKey = CacheKeys.ProfileEducation(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.EducationExperiences
                .AsNoTracking()
                .Where(e => e.PersonalProfileId == profileId)
                .OrderByDescending(e => e.StartDate)
                .Select(e => new EducationExperienceDto
                {
                    Id = e.Id,
                    SchoolName = e.SchoolName,
                    Degree = e.Degree,
                    Major = e.Major,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    Logo = new AssetDto { Type = e.Logo.Type, Value = e.Logo.Value, Tag = e.Logo.Tag }
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        return ApiResponse<IEnumerable<EducationExperienceDto>>.Success(list ?? new List<EducationExperienceDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddEducationAsync(Guid accountId, UpdateEducationExperienceRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Logo, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var isPersonal = await _context.PersonalProfiles.AnyAsync(p => p.Id == accountId);
        if (!isPersonal) return ApiResponse<MessageResponse>.Failure("Operation valid only for personal accounts.");

        var entity = new EducationExperience
        {
            PersonalProfileId = accountId,
            SchoolName = dto.SchoolName,
            Degree = dto.Degree,
            Major = dto.Major,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Logo = dto.Logo != null ? new Asset { Type = dto.Logo.Type, Value = dto.Logo.Value, Tag = dto.Logo.Tag } : new Asset()
        };

        _context.EducationExperiences.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileEducation(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Education added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateEducationAsync(Guid accountId, Guid educationId, UpdateEducationExperienceRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Logo, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var entity = await _context.EducationExperiences.FirstOrDefaultAsync(e => e.Id == educationId && e.PersonalProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        entity.SchoolName = dto.SchoolName;
        if (dto.Degree != null) entity.Degree = dto.Degree;
        if (dto.Major != null) entity.Major = dto.Major;
        if (dto.StartDate != null) entity.StartDate = dto.StartDate;
        if (dto.EndDate != null) entity.EndDate = dto.EndDate;
        
        if (dto.Logo != null)
        {
            entity.Logo = new Asset { Type = dto.Logo.Type, Value = dto.Logo.Value, Tag = dto.Logo.Tag };
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileEducation(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Education updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteEducationAsync(Guid accountId, Guid educationId)
    {
        var entity = await _context.EducationExperiences.FirstOrDefaultAsync(e => e.Id == educationId && e.PersonalProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.EducationExperiences.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileEducation(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Education deleted."));
    }

    public async Task<ApiResponse<IEnumerable<ProjectDto>>> GetProjectsAsync(Guid profileId, bool publicOnly = false)
    {
        var cacheKey = CacheKeys.ProfileProjects(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.Projects
                .AsNoTracking()
                .Where(p => p.ProfileId == profileId)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new ProjectDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Summary = p.Summary,
                    Content = p.Content,
                    Url = p.Url,
                    DisplayOrder = p.DisplayOrder,
                    Visibility = p.Visibility,
                    Logo = new AssetDto { Type = p.Logo.Type, Value = p.Logo.Value, Tag = p.Logo.Tag }
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        if (list != null && publicOnly)
        {
            list = list.Where(p => p.Visibility == Visibility.Public).ToList();
        }

        return ApiResponse<IEnumerable<ProjectDto>>.Success(list ?? new List<ProjectDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddProjectAsync(Guid accountId, UpdateProjectRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Logo, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var entity = new Project
        {
            ProfileId = accountId,
            Name = dto.Name ?? "New Project",
            Summary = dto.Summary,
            Content = dto.Content,
            Url = dto.Url,
            DisplayOrder = dto.DisplayOrder ?? 0,
            Visibility = dto.Visibility ?? Visibility.Public,
            Logo = dto.Logo != null ? new Asset { Type = dto.Logo.Type, Value = dto.Logo.Value, Tag = dto.Logo.Tag } : new Asset()
        };

        _context.Projects.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileProjects(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Project added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateProjectAsync(Guid accountId, Guid projectId, UpdateProjectRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Logo, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var entity = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        if (dto.Name != null) entity.Name = dto.Name;
        if (dto.Summary != null) entity.Summary = dto.Summary;
        if (dto.Content != null) entity.Content = dto.Content;
        if (dto.Url != null) entity.Url = dto.Url;
        if (dto.DisplayOrder != null) entity.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.Visibility != null) entity.Visibility = dto.Visibility.Value;
        
        if (dto.Logo != null)
        {
            entity.Logo = new Asset { Type = dto.Logo.Type, Value = dto.Logo.Value, Tag = dto.Logo.Tag };
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileProjects(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Project updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteProjectAsync(Guid accountId, Guid projectId)
    {
        var entity = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.Projects.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileProjects(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Project deleted."));
    }

    public async Task<ApiResponse<IEnumerable<SocialLinkDto>>> GetSocialsAsync(Guid profileId)
    {
        var cacheKey = CacheKeys.ProfileSocials(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.SocialLinks
                .AsNoTracking()
                .Where(s => s.ProfileId == profileId)
                .Select(s => new SocialLinkDto
                {
                    Id = s.Id,
                    Platform = s.Platform,
                    Url = s.Url,
                    Icon = new AssetDto { Type = s.Icon.Type, Value = s.Icon.Value, Tag = s.Icon.Tag }
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        return ApiResponse<IEnumerable<SocialLinkDto>>.Success(list ?? new List<SocialLinkDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddSocialAsync(Guid accountId, UpdateSocialLinkRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Icon, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        if (string.IsNullOrWhiteSpace(dto.Platform) || string.IsNullOrWhiteSpace(dto.Url))
            return ApiResponse<MessageResponse>.Failure("Platform and URL are required.");

        var entity = new SocialLink
        {
            ProfileId = accountId,
            Platform = dto.Platform,
            Url = dto.Url,
            Icon = dto.Icon != null ? new Asset { Type = dto.Icon.Type, Value = dto.Icon.Value, Tag = dto.Icon.Tag } : new Asset()
        };

        _context.SocialLinks.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileSocials(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Social link added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateSocialAsync(Guid accountId, Guid linkId, UpdateSocialLinkRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Icon, SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var entity = await _context.SocialLinks.FirstOrDefaultAsync(s => s.Id == linkId && s.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        if (dto.Platform != null) entity.Platform = dto.Platform;
        if (dto.Url != null) entity.Url = dto.Url;
        
        if (dto.Icon != null)
        {
            entity.Icon = new Asset { Type = dto.Icon.Type, Value = dto.Icon.Value, Tag = dto.Icon.Tag };
        }

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileSocials(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Social link updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteSocialAsync(Guid accountId, Guid linkId)
    {
        var entity = await _context.SocialLinks.FirstOrDefaultAsync(s => s.Id == linkId && s.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.SocialLinks.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileSocials(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Social link deleted."));
    }
    
    public async Task<ApiResponse<IEnumerable<ContactMethodDto>>> GetContactsAsync(Guid profileId, bool publicOnly = false)
    {
        var cacheKey = CacheKeys.ProfileContacts(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.ContactMethods
                .AsNoTracking()
                .Where(c => c.ProfileId == profileId)
                .Select(c => new ContactMethodDto
                {
                    Id = c.Id,
                    Type = c.Type,
                    Label = c.Label,
                    Value = c.Value,
                    Visibility = c.Visibility,
                    Icon = new AssetDto { Type = c.Icon.Type, Value = c.Icon.Value, Tag = c.Icon.Tag },
                    Image = new AssetDto { Type = c.Image.Type, Value = c.Image.Value, Tag = c.Image.Tag }
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        if (list != null && publicOnly)
        {
            list = list.Where(c => c.Visibility == Visibility.Public).ToList();
        }

        return ApiResponse<IEnumerable<ContactMethodDto>>.Success(list ?? new List<ContactMethodDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddContactAsync(Guid accountId, UpdateContactMethodRequestDto dto)
    {
        int limit = await _settingService.GetIntAsync(SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        var vIcon = AssetValidator.Validate(dto.Icon, limit);
        if (!vIcon.Valid) return ApiResponse<MessageResponse>.Failure(vIcon.Error!);

        var entity = new ContactMethod
        {
            ProfileId = accountId,
            Type = dto.Type ?? ContactType.Email,
            Label = dto.Label ?? string.Empty,
            Value = dto.Value,
            Visibility = dto.Visibility ?? Visibility.Private,
            Icon = dto.Icon != null ? new Asset { Type = dto.Icon.Type, Value = dto.Icon.Value, Tag = dto.Icon.Tag } : new Asset(),
            Image = dto.Image != null ? new Asset { Type = dto.Image.Type, Value = dto.Image.Value, Tag = dto.Image.Tag } : new Asset()
        };

        _context.ContactMethods.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileContacts(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Contact method added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateContactAsync(Guid accountId, Guid contactId, UpdateContactMethodRequestDto dto)
    {
        var entity = await _context.ContactMethods.FirstOrDefaultAsync(c => c.Id == contactId && c.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        if (dto.Type.HasValue) entity.Type = dto.Type.Value;
        if (dto.Label != null) entity.Label = dto.Label;
        if (dto.Value != null) entity.Value = dto.Value;
        if (dto.Visibility.HasValue) entity.Visibility = dto.Visibility.Value;
        
        if (dto.Icon != null)
            entity.Icon = new Asset { Type = dto.Icon.Type, Value = dto.Icon.Value, Tag = dto.Icon.Tag };
        
        if (dto.Image != null)
            entity.Image = new Asset { Type = dto.Image.Type, Value = dto.Image.Value, Tag = dto.Image.Tag };

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileContacts(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Contact method updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteContactAsync(Guid accountId, Guid contactId)
    {
        var entity = await _context.ContactMethods.FirstOrDefaultAsync(c => c.Id == contactId && c.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.ContactMethods.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileContacts(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Contact method removed."));
    }

    
    public async Task<ApiResponse<IEnumerable<PublicOrganizationMembershipDto>>> GetPublicMembershipsAsync(Guid profileId)
    {
        var cacheKey = CacheKeys.ProfileMemberships(profileId);

        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.AccountId == profileId)
                .Where(m => m.Visibility == Visibility.Public)
                .Include(m => m.Organization)
                .ThenInclude(o => o.Account)
                .Where(m => m.Organization.Account.Status == AccountStatus.Active)
                .OrderByDescending(m => m.JoinedAt)
                .Select(m => new PublicOrganizationMembershipDto
                {
                    OrganizationId = m.OrganizationId,
                    AccountName = m.Organization.Account.AccountName,
                    DisplayName = m.Organization.DisplayName,
                    Avatar = new AssetDto 
                    { 
                        Type = m.Organization.Avatar.Type, 
                        Value = m.Organization.Avatar.Value,
                        Tag = m.Organization.Avatar.Tag
                    },
                    Role = m.Role,
                    Title = m.Title,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        return ApiResponse<IEnumerable<PublicOrganizationMembershipDto>>.Success(list ?? new List<PublicOrganizationMembershipDto>());
    }

    public async Task<ApiResponse<IEnumerable<OrganizationMemberDto>>> GetPublicOrgMembersAsync(Guid orgId)
    {
        var cacheKey = CacheKeys.OrganizationMembers(orgId);

        var members = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.OrganizationMembers
                .AsNoTracking()
                .Where(m => m.OrganizationId == orgId && m.Visibility == Visibility.Public)
                .Include(m => m.Account).ThenInclude(a => a.Profile)
                .Where(m => m.Account.Status == AccountStatus.Active)
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
        }, tags: [cacheKey]);

        return ApiResponse<IEnumerable<OrganizationMemberDto>>.Success(members ?? new List<OrganizationMemberDto>());
    }


    // ==========================================
    // Certificates
    // ==========================================

    public async Task<ApiResponse<IEnumerable<CertificateDto>>> GetCertificatesAsync(Guid profileId, bool publicOnly = false)
    {
        var cacheKey = CacheKeys.ProfileCertificates(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.Certificates
                .AsNoTracking()
                .Where(c => c.ProfileId == profileId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CertificateDto
                {
                    Id = c.Id,
                    Type = c.Type,
                    Name = c.Name,
                    Fingerprint = c.Fingerprint,
                    Email = c.Email,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    ExpiresAt = c.ExpiresAt,
                    Visibility = c.Visibility
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        if (list != null && publicOnly)
        {
            list = list.Where(c => c.Visibility == Visibility.Public).ToList();
        }

        return ApiResponse<IEnumerable<CertificateDto>>.Success(list ?? new List<CertificateDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddCertificateAsync(Guid accountId, UpdateCertificateRequestDto dto)
    {
        var entity = new Certificate
        {
            ProfileId = accountId,
            Type = dto.Type,
            Name = dto.Name,
            Fingerprint = dto.Fingerprint,
            Email = dto.Email,
            Content = dto.Content,
            CreatedAt = dto.CreatedAt ?? DateTime.UtcNow,
            ExpiresAt = dto.ExpiresAt,
            Visibility = dto.Visibility ?? Visibility.Public
        };

        _context.Certificates.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileCertificates(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Certificate added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateCertificateAsync(Guid accountId, Guid certId, UpdateCertificateRequestDto dto)
    {
        var entity = await _context.Certificates.FirstOrDefaultAsync(c => c.Id == certId && c.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        if (dto.Type != null) entity.Type = dto.Type;
        if (dto.Name != null) entity.Name = dto.Name;
        if (dto.Fingerprint != null) entity.Fingerprint = dto.Fingerprint;
        if (dto.Email != null) entity.Email = dto.Email;
        if (dto.Content != null) entity.Content = dto.Content;
        if (dto.CreatedAt != null) entity.CreatedAt = dto.CreatedAt;
        if (dto.ExpiresAt != null) entity.ExpiresAt = dto.ExpiresAt;
        if (dto.Visibility != null) entity.Visibility = dto.Visibility.Value;

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileCertificates(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Certificate updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteCertificateAsync(Guid accountId, Guid certId)
    {
        var entity = await _context.Certificates.FirstOrDefaultAsync(c => c.Id == certId && c.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.Certificates.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileCertificates(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Certificate deleted."));
    }

    // ==========================================
    // Sponsorships
    // ==========================================

    public async Task<ApiResponse<IEnumerable<SponsorshipItemDto>>> GetSponsorshipsAsync(Guid profileId, bool publicOnly = false)
    {
        var cacheKey = CacheKeys.ProfileSponsorships(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.SponsorshipItems
                .AsNoTracking()
                .Where(s => s.ProfileId == profileId)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new SponsorshipItemDto
                {
                    Id = s.Id,
                    Platform = s.Platform,
                    Url = s.Url,
                    Icon = new AssetDto { Type = s.Icon.Type, Value = s.Icon.Value, Tag = s.Icon.Tag },
                    QrCode = new AssetDto { Type = s.QrCode.Type, Value = s.QrCode.Value, Tag = s.QrCode.Tag },
                    DisplayOrder = s.DisplayOrder,
                    Visibility = s.Visibility
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        if (list != null && publicOnly)
        {
            list = list.Where(s => s.Visibility == Visibility.Public).ToList();
        }

        return ApiResponse<IEnumerable<SponsorshipItemDto>>.Success(list ?? new List<SponsorshipItemDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddSponsorshipAsync(Guid accountId, UpdateSponsorshipItemRequestDto dto)
    {
        int limit = await _settingService.GetIntAsync(SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        
        var vIcon = AssetValidator.Validate(dto.Icon, limit);
        if (!vIcon.Valid) return ApiResponse<MessageResponse>.Failure(vIcon.Error!);

        var vQr = AssetValidator.Validate(dto.QrCode, limit);
        if (!vQr.Valid) return ApiResponse<MessageResponse>.Failure(vQr.Error!);

        var entity = new SponsorshipItem
        {
            ProfileId = accountId,
            Platform = dto.Platform,
            Url = dto.Url,
            DisplayOrder = dto.DisplayOrder ?? 0,
            Visibility = dto.Visibility ?? Visibility.Public,
            Icon = dto.Icon != null ? new Asset { Type = dto.Icon.Type, Value = dto.Icon.Value, Tag = dto.Icon.Tag } : new Asset(),
            QrCode = dto.QrCode != null ? new Asset { Type = dto.QrCode.Type, Value = dto.QrCode.Value, Tag = dto.QrCode.Tag } : new Asset()
        };

        _context.SponsorshipItems.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileSponsorships(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Sponsorship added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateSponsorshipAsync(Guid accountId, Guid itemId, UpdateSponsorshipItemRequestDto dto)
    {
        int limit = await _settingService.GetIntAsync(SystemSettingKeys.MaxAssetSizeBytes, 5242880);
        
        var vIcon = AssetValidator.Validate(dto.Icon, limit);
        if (!vIcon.Valid) return ApiResponse<MessageResponse>.Failure(vIcon.Error!);

        var vQr = AssetValidator.Validate(dto.QrCode, limit);
        if (!vQr.Valid) return ApiResponse<MessageResponse>.Failure(vQr.Error!);

        var entity = await _context.SponsorshipItems.FirstOrDefaultAsync(s => s.Id == itemId && s.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        if (dto.Platform != null) entity.Platform = dto.Platform;
        if (dto.Url != null) entity.Url = dto.Url;
        if (dto.DisplayOrder != null) entity.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.Visibility != null) entity.Visibility = dto.Visibility.Value;
        
        if (dto.Icon != null)
            entity.Icon = new Asset { Type = dto.Icon.Type, Value = dto.Icon.Value, Tag = dto.Icon.Tag };
        
        if (dto.QrCode != null)
            entity.QrCode = new Asset { Type = dto.QrCode.Type, Value = dto.QrCode.Value, Tag = dto.QrCode.Tag };

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileSponsorships(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Sponsorship updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteSponsorshipAsync(Guid accountId, Guid itemId)
    {
        var entity = await _context.SponsorshipItems.FirstOrDefaultAsync(s => s.Id == itemId && s.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.SponsorshipItems.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileSponsorships(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Sponsorship deleted."));
    }

    // ==========================================
    // Gallery
    // ==========================================

    public async Task<ApiResponse<IEnumerable<GalleryItemDto>>> GetGalleryAsync(Guid profileId, bool publicOnly = false)
    {
        var cacheKey = CacheKeys.ProfileGallery(profileId);
        
        var list = await _cache.GetOrSetAsync(cacheKey, async _ =>
        {
            return await _context.GalleryItems
                .AsNoTracking()
                .Where(g => g.ProfileId == profileId)
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new GalleryItemDto
                {
                    Id = g.Id,
                    Image = new AssetDto { Type = g.Image.Type, Value = g.Image.Value, Tag = g.Image.Tag },
                    Caption = g.Caption,
                    ActionUrl = g.ActionUrl,
                    DisplayOrder = g.DisplayOrder,
                    Visibility = g.Visibility
                })
                .ToListAsync();
        }, tags: [cacheKey]);

        if (list != null && publicOnly)
        {
            list = list.Where(g => g.Visibility == Visibility.Public).ToList();
        }

        return ApiResponse<IEnumerable<GalleryItemDto>>.Success(list ?? new List<GalleryItemDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddGalleryItemAsync(Guid accountId, UpdateGalleryItemRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Image, SystemSettingKeys.MaxGalleryAssetSizeBytes, 10485760);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        if (dto.Image == null) return ApiResponse<MessageResponse>.Failure("Image is required.");

        var entity = new GalleryItem
        {
            ProfileId = accountId,
            Image = new Asset { Type = dto.Image.Type, Value = dto.Image.Value, Tag = dto.Image.Tag },
            Caption = dto.Caption,
            ActionUrl = dto.ActionUrl,
            DisplayOrder = dto.DisplayOrder ?? 0,
            Visibility = dto.Visibility ?? Visibility.Public
        };

        _context.GalleryItems.Add(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileGallery(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Gallery item added."));
    }

    public async Task<ApiResponse<MessageResponse>> UpdateGalleryItemAsync(Guid accountId, Guid itemId, UpdateGalleryItemRequestDto dto)
    {
        var assetError = await ValidateAssetAsync(dto.Image, SystemSettingKeys.MaxGalleryAssetSizeBytes, 10485760);
        if (assetError != null) return ApiResponse<MessageResponse>.Failure(assetError);

        var entity = await _context.GalleryItems.FirstOrDefaultAsync(g => g.Id == itemId && g.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        if (dto.Caption != null) entity.Caption = dto.Caption;
        if (dto.ActionUrl != null) entity.ActionUrl = dto.ActionUrl;
        if (dto.DisplayOrder != null) entity.DisplayOrder = dto.DisplayOrder.Value;
        if (dto.Visibility != null) entity.Visibility = dto.Visibility.Value;
        
        if (dto.Image != null)
            entity.Image = new Asset { Type = dto.Image.Type, Value = dto.Image.Value, Tag = dto.Image.Tag };

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileGallery(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Gallery item updated."));
    }

    public async Task<ApiResponse<MessageResponse>> DeleteGalleryItemAsync(Guid accountId, Guid itemId)
    {
        var entity = await _context.GalleryItems.FirstOrDefaultAsync(g => g.Id == itemId && g.ProfileId == accountId);
        if (entity == null) return ApiResponse<MessageResponse>.Failure("Item not found.");

        _context.GalleryItems.Remove(entity);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.ProfileGallery(accountId));

        return ApiResponse<MessageResponse>.Success(MessageResponse.Create("Gallery item deleted."));
    }
}
