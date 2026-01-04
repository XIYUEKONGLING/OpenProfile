using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.DTOs.Common;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.DTOs.Site;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Generator;

public class StaticGenerator
{
    private readonly ApplicationDbContext _context;
    private readonly IProfileService _profileService;
    private readonly IProfileDetailService _detailService;
    private readonly ISocialService _socialService;
    private readonly ISiteMetadataService _metaService;
    private readonly ISystemSettingService _settingService;
    private readonly ApplicationOptions _appOptions;
    private readonly ILogger<StaticGenerator> _logger;
    
    private readonly JsonSerializerOptions _jsonOptions;

    public StaticGenerator(
        ApplicationDbContext context,
        IProfileService profileService,
        IProfileDetailService detailService,
        ISocialService socialService,
        ISiteMetadataService metaService,
        ISystemSettingService settingService,
        IOptions<ApplicationOptions> appOptions,
        ILogger<StaticGenerator> logger)
    {
        _context = context;
        _profileService = profileService;
        _detailService = detailService;
        _socialService = socialService;
        _metaService = metaService;
        _settingService = settingService;
        _appOptions = appOptions.Value;
        _logger = logger;

        // Match the main API's JSON configuration (PascalCase, no indentation for smaller file size)
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // Ensure PascalCase matches DTOs
            WriteIndented = false
        };
    }

    public async Task GenerateAsync(string rootPath)
    {
        // Clean and prepare output directory
        var apiPath = Path.Combine(rootPath, "api");
        if (Directory.Exists(apiPath))
        {
            Directory.Delete(apiPath, true);
        }
        Directory.CreateDirectory(apiPath);

        // 1. Generate Global Metadata (Index, Info, Features)
        await GenerateSystemInfoAsync(apiPath);

        // 2. Generate Profiles
        await GenerateProfilesAsync(apiPath);
    }

    private async Task GenerateSystemInfoAsync(string basePath)
    {
        _logger.LogInformation("Generating system metadata...");

        var serverInfo = new ServerInfoDto
        {
            Version = _appOptions.Version,
            Server = ApplicationInformation.GeneratorName,
            Static = true,
            Dynamic = false
        };

        var meta = await _metaService.GetMetadataAsync();
        var metaDto = new SiteMetadataDto
        {
            SiteName = meta.SiteName,
            SiteDescription = meta.SiteDescription,
            Copyright = meta.Copyright,
            ContactEmail = meta.ContactEmail,
            Logo = new AssetDto { Type = meta.Logo.Type, Value = meta.Logo.Value, Tag = meta.Logo.Tag },
            Favicon = new AssetDto { Type = meta.Favicon.Type, Value = meta.Favicon.Value, Tag = meta.Favicon.Tag }
        };

        // Static mode implies interactive features are disabled
        var features = new ServerFeaturesDto
        {
            Email = false,
            Registration = false,
            EmailVerification = false,
            EmailAddVerification = false,
            SearchIndexing = await _settingService.GetBoolAsync(SystemSettingKeys.AllowSearchEngineIndexing, true)
        };

        var combined = new ServerResponseDto
        {
            ServerInfo = serverInfo,
            SiteMeta = metaDto,
            Features = features
        };

        // Write endpoints mimicking /api, /api/info, /api/meta
        await WriteJsonAsync(Path.Combine(basePath, "index.json"), ApiResponse<ServerResponseDto>.Success(combined));
        await WriteJsonAsync(Path.Combine(basePath, "info.json"), ApiResponse<ServerInfoDto>.Success(serverInfo));
        await WriteJsonAsync(Path.Combine(basePath, "meta.json"), ApiResponse<SiteMetadataDto>.Success(metaDto));
        await WriteJsonAsync(Path.Combine(basePath, "features.json"), ApiResponse<ServerFeaturesDto>.Success(features));
    }

    private async Task GenerateProfilesAsync(string basePath)
    {
        var profilesDir = Path.Combine(basePath, "profiles");
        Directory.CreateDirectory(profilesDir);

        // Fetch all active accounts
        // We do not export Banned or PendingDeletion accounts
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Status == AccountStatus.Active)
            .Select(a => new { a.Id, a.AccountName, a.Type })
            .ToListAsync();

        _logger.LogInformation("Found {Count} active profiles to export.", accounts.Count);

        var tasks = accounts.Select(async acc =>
        {
            try
            {
                // Generate files for BOTH the UUID (e.g. @guid.json) and AccountName (e.g. alice.json)
                // This supports both routing styles without a backend router.
                await GenerateSingleProfileAsync(profilesDir, $"@{acc.Id}", acc.Id, acc.Type);
                await GenerateSingleProfileAsync(profilesDir, acc.AccountName, acc.Id, acc.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate profile for {AccountName} ({Id})", acc.AccountName, acc.Id);
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task GenerateSingleProfileAsync(string profilesDir, string identifier, Guid accountId, AccountType type)
    {
        // 1. Main Profile JSON: /api/profiles/{identifier}.json
        var profilePath = Path.Combine(profilesDir, $"{identifier}.json");
        
        // Use the service to get the official DTO (ensures consistent mapping logic)
        // We pass the UUID string to ResolveIdAsync effectively works
        var profileResponse = await _profileService.GetProfileAsync(accountId.ToString());
        
        if (!profileResponse.Status) return; // Should not happen given we queried active accounts

        await WriteJsonAsync(profilePath, profileResponse);

        // 2. Sub-Resources Directory: /api/profiles/{identifier}/
        var subResDir = Path.Combine(profilesDir, identifier);
        Directory.CreateDirectory(subResDir);

        // 3. Common Sub-Resources
        await ExportSubResource(subResDir, "socials", () => _detailService.GetSocialsAsync(accountId));
        await ExportSubResource(subResDir, "gallery", () => _detailService.GetGalleryAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "contacts", () => _detailService.GetContactsAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "certificates", () => _detailService.GetCertificatesAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "sponsorships", () => _detailService.GetSponsorshipsAsync(accountId, publicOnly: true));
        
        // 4. Social Lists
        await ExportSubResource(subResDir, "followers", () => _socialService.GetFollowersAsync(accountId));
        await ExportSubResource(subResDir, "following", () => _socialService.GetFollowingAsync(accountId));
        
        // Privacy settings are public for the frontend to determine what UI elements to render/hide
        await ExportSubResource(subResDir, "privacy", () => _profileService.GetProfilePrivacyAsync(accountId.ToString()));

        // 5. Type Specific Sub-Resources
        if (type == AccountType.Personal)
        {
            await ExportSubResource(subResDir, "work", () => _detailService.GetWorkAsync(accountId, publicOnly: true));
            await ExportSubResource(subResDir, "education", () => _detailService.GetEducationAsync(accountId, publicOnly: true));
            await ExportSubResource(subResDir, "projects", () => _detailService.GetProjectsAsync(accountId, publicOnly: true));
            await ExportSubResource(subResDir, "memberships", () => _detailService.GetPublicMembershipsAsync(accountId));
        }
        else if (type == AccountType.Organization)
        {
            await ExportSubResource(subResDir, "members", () => _detailService.GetPublicOrgMembersAsync(accountId));
        }
    }

    private async Task ExportSubResource<T>(string directory, string filename, Func<Task<ApiResponse<T>>> fetcher)
    {
        try
        {
            var response = await fetcher();
            var path = Path.Combine(directory, $"{filename}.json");
            await WriteJsonAsync(path, response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to export sub-resource {Resource} in {Directory}: {Message}", filename, directory, ex.Message);
        }
    }

    private async Task WriteJsonAsync<T>(string path, T data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
