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
    private readonly IAssetService _assetService;
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
        IAssetService assetService,
        IOptions<ApplicationOptions> appOptions,
        ILogger<StaticGenerator> logger)
    {
        _context = context;
        _profileService = profileService;
        _detailService = detailService;
        _socialService = socialService;
        _metaService = metaService;
        _settingService = settingService;
        _assetService = assetService;
        _appOptions = appOptions.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false
        };
    }

    public async Task GenerateAsync(string rootPath)
    {
        var apiPath = Path.Combine(rootPath, "api");
        if (Directory.Exists(apiPath))
        {
            Directory.Delete(apiPath, true);
        }
        Directory.CreateDirectory(apiPath);

        _logger.LogInformation("Generating System Metadata...");
        await GenerateSystemInfoAsync(apiPath);

        _logger.LogInformation("Generating Profiles...");
        await GenerateProfilesAsync(apiPath);

        _logger.LogInformation("Generating Public Asset Library...");
        await GenerateAssetLibraryAsync(apiPath);
    }

    private async Task GenerateSystemInfoAsync(string basePath)
    {
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

        var features = new ServerFeaturesDto
        {
            Maintenance = false,
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

        await WriteJsonAsync(Path.Combine(basePath, "index.json"), ApiResponse<ServerResponseDto>.Success(combined));
        await WriteJsonAsync(Path.Combine(basePath, "info.json"), ApiResponse<ServerInfoDto>.Success(serverInfo));
        await WriteJsonAsync(Path.Combine(basePath, "meta.json"), ApiResponse<SiteMetadataDto>.Success(metaDto));
        await WriteJsonAsync(Path.Combine(basePath, "features.json"), ApiResponse<ServerFeaturesDto>.Success(features));
    }

    private async Task GenerateProfilesAsync(string basePath)
    {
        var profilesDir = Path.Combine(basePath, "profiles");
        Directory.CreateDirectory(profilesDir);

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Status == AccountStatus.Active || a.Status == AccountStatus.Suspended || a.Status == AccountStatus.Banned)
            .Select(a => new { a.Id, a.AccountName, a.Type })
            .ToListAsync();

        if (accounts.Count == 0)
        {
            _logger.LogWarning("No active accounts found. Profiles directory will be empty.");
            return;
        }

        _logger.LogInformation("Found {Count} active profiles. Starting export...", accounts.Count);

        int count = 0;
        foreach (var acc in accounts)
        {
            try
            {
                count++;
                _logger.LogInformation("[{Current}/{Total}] Exporting {AccountName}...", count, accounts.Count, acc.AccountName);

                // Export via UUID (@guid.json)
                await GenerateSingleProfileAsync(profilesDir, $"@{acc.Id}", acc.Id, acc.Type);
                
                // Export via AccountName (username.json)
                await GenerateSingleProfileAsync(profilesDir, acc.AccountName, acc.Id, acc.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate profile for {AccountName} ({Id})", acc.AccountName, acc.Id);
            }
        }
    }

    private async Task GenerateSingleProfileAsync(string profilesDir, string identifier, Guid accountId, AccountType type)
    {
        // 1. Main Profile JSON
        var profilePath = Path.Combine(profilesDir, $"{identifier}.json");
        
        // FIX: Add '@' prefix to force ProfileService to treat it as a UUID.
        // Otherwise it looks for a user with username "1234-5678..." and fails.
        var profileResponse = await _profileService.GetProfileAsync($"@{accountId}");
        
        if (!profileResponse.Status) 
        {
            _logger.LogWarning("Skipping {Identifier}: Service returned failure (Profile not found).", identifier);
            return; 
        }

        await WriteJsonAsync(profilePath, profileResponse);

        // 2. Sub-Resources Directory
        var subResDir = Path.Combine(profilesDir, identifier);
        Directory.CreateDirectory(subResDir);

        // 3. Common Sub-Resources
        await ExportSubResource(subResDir, "socials", () => _detailService.GetSocialsAsync(accountId));
        await ExportSubResource(subResDir, "gallery", () => _detailService.GetGalleryAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "contacts", () => _detailService.GetContactsAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "certificates", () => _detailService.GetCertificatesAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "sponsorships", () => _detailService.GetSponsorshipsAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "projects", () => _detailService.GetProjectsAsync(accountId, publicOnly: true));
        await ExportSubResource(subResDir, "memberships", () => _detailService.GetPublicMembershipsAsync(accountId));
        
        // 4. Social Lists
        await ExportSubResource(subResDir, "followers", () => _socialService.GetFollowersAsync(accountId));
        await ExportSubResource(subResDir, "following", () => _socialService.GetFollowingAsync(accountId));
        await ExportSubResource(subResDir, "privacy", () => _profileService.GetProfilePrivacyAsync($"@{accountId}"));

        await ExportSubResource(subResDir, "created-at", () => _profileService.GetAccountCreatedDateAsync($"@{accountId}"));
        
        // 5. Type Specific Sub-Resources
        if (type == AccountType.Personal)
        {
            await ExportSubResource(subResDir, "work", () => _detailService.GetWorkAsync(accountId, publicOnly: true));
            await ExportSubResource(subResDir, "education", () => _detailService.GetEducationAsync(accountId, publicOnly: true));
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

    private async Task GenerateAssetLibraryAsync(string basePath)
    {
        var assetsDir = Path.Combine(basePath, "assets");
        Directory.CreateDirectory(assetsDir);

        // Get all public assets from AccountAssets (user and organization assets)
        var publicAccountAssets = await _context.AccountAssets
            .AsNoTracking()
            .Include(a => a.Account)
            .Where(a => a.Visibility == Visibility.Public)
            .Select(a => new { a.Id, a.Account.Status })
            .ToListAsync();

        // Get all public system assets
        var publicSystemAssets = await _context.SystemAssets
            .AsNoTracking()
            .Where(a => a.Visibility == Visibility.Public)
            .Select(a => a.Id)
            .ToListAsync();

        int totalAssets = publicAccountAssets.Count + publicSystemAssets.Count;
        if (totalAssets == 0)
        {
            _logger.LogInformation("No public assets found.");
            return;
        }

        _logger.LogInformation("Found {AccountCount} account assets and {SystemCount} system assets. Starting export...",
            publicAccountAssets.Count, publicSystemAssets.Count);

        int count = 0;

        // Export account assets
        foreach (var asset in publicAccountAssets)
        {
            try
            {
                // Skip if account is not active
                if (asset.Status != AccountStatus.Active)
                {
                    _logger.LogDebug("Skipping account asset {AssetId}: Account is not active.", asset.Id);
                    continue;
                }

                count++;
                var assetResponse = await _assetService.GetPublicAssetAsync(asset.Id);

                if (assetResponse.Status)
                {
                    var assetPath = Path.Combine(assetsDir, $"{asset.Id}.json");
                    await WriteJsonAsync(assetPath, assetResponse);
                }
                else
                {
                    _logger.LogDebug("Skipping account asset {AssetId}: Service returned failure.", asset.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate account asset {AssetId}", asset.Id);
            }
        }

        // Export system assets
        foreach (var assetId in publicSystemAssets)
        {
            try
            {
                count++;
                var assetResponse = await _assetService.GetPublicAssetAsync(assetId);

                if (assetResponse.Status)
                {
                    var assetPath = Path.Combine(assetsDir, $"{assetId}.json");
                    await WriteJsonAsync(assetPath, assetResponse);
                }
                else
                {
                    _logger.LogDebug("Skipping system asset {AssetId}: Service returned failure.", assetId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate system asset {AssetId}", assetId);
            }
        }

        _logger.LogInformation("Exported {Count} public assets.", count);
    }
}
