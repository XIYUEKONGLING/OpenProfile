using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Profile.Details;
using OpenProfileServer.Models.Entities.Details;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;
using ZiggyCreatures.Caching.Fusion;

namespace OpenProfileServer.Services;

public class ProfileDetailService : IProfileDetailService
{
    private readonly ApplicationDbContext _context;
    private readonly IFusionCache _cache;

    public ProfileDetailService(ApplicationDbContext context, IFusionCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // ==========================================
    // Work Experience
    // ==========================================

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

        // Note: WorkExperience currently does not have a "Visibility" column in the Entity model provided.
        // Assuming they are public by default if the profile is public.
        // If Visibility was added to WorkExperience entity, we would filter here:
        // if (publicOnly) return ApiResponse<...>.Success(list.Where(x => x.Visibility == Visibility.Public));

        return ApiResponse<IEnumerable<WorkExperienceDto>>.Success(list ?? new List<WorkExperienceDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddWorkAsync(Guid accountId, UpdateWorkExperienceRequestDto dto)
    {
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

    // ==========================================
    // Education
    // ==========================================

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

    // ==========================================
    // Projects
    // ==========================================

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

        // Filter Logic Implementation
        if (list != null && publicOnly)
        {
            list = list.Where(p => p.Visibility == Visibility.Public).ToList();
        }

        return ApiResponse<IEnumerable<ProjectDto>>.Success(list ?? new List<ProjectDto>());
    }

    public async Task<ApiResponse<MessageResponse>> AddProjectAsync(Guid accountId, UpdateProjectRequestDto dto)
    {
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

    // ==========================================
    // Social Links
    // ==========================================

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
}
