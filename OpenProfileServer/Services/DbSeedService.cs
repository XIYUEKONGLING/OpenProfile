using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenProfileServer.Configuration;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Models.DTOs.Core;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Utilities;

namespace OpenProfileServer.Services;

public class DbSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly SecurityOptions _securityOptions;

    public DbSeedService(ApplicationDbContext context, IOptions<SecurityOptions> securityOptions)
    {
        _context = context;
        _securityOptions = securityOptions.Value;
    }

    public async Task SeedAsync()
    {
        await SeedSystemSettingsAsync();
        await SeedRootAccountAsync();
    }

    private async Task SeedSystemSettingsAsync()
    {
        // Default logo configuration
        var defaultLogo = new AssetDto
        {
            Type = AssetType.Text,
            Value = "\U0001F464",
            Tag = "Default Site Logo"
        };

        var defaults = new Dictionary<string, (string Value, string Type, string Desc)>
        {
            { 
                SystemSettingKeys.AllowRegistration, 
                ("true", "boolean", "Allows new users to register.") 
            },
            { 
                SystemSettingKeys.RequireEmailVerification, 
                ("false", "boolean", "Requires users to verify email before full access.") 
            },
            { 
                SystemSettingKeys.MaintenanceMode, 
                ("false", "boolean", "Puts the server in maintenance mode.") 
            },
            { 
                SystemSettingKeys.AllowSearchEngineIndexing, 
                ("true", "boolean", "Enable robots.txt indexing.") 
            },
            { 
                SystemSettingKeys.SiteName, 
                ("OpenProfile", "string", "Site title.") 
            },
            { 
                SystemSettingKeys.SiteDescription, 
                ("Identity management platform.", "string", "Site tagline.") 
            },
            { 
                SystemSettingKeys.ContactEmail, 
                ("admin@localhost", "string", "Public contact address.") 
            },
            { 
                SystemSettingKeys.SiteLogo, 
                (JsonConvert.SerializeObject(defaultLogo), "json", "Site branding asset.") 
            }
        };

        foreach (var (key, (value, type, desc)) in defaults)
        {
            if (!await _context.SystemSettings.AnyAsync(s => s.Key == key))
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    ValueType = type,
                    Description = desc,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        
        await _context.SaveChangesAsync();
    }

    private async Task SeedRootAccountAsync()
    {
        var rootExists = await _context.Accounts.AnyAsync(a => a.Role == AccountRole.Root);
        if (rootExists) return;

        var (hash, salt) = CryptographyProvider.CreateHash(_securityOptions.RootPassword);
        var accountId = Guid.NewGuid();

        var rootAccount = new Account
        {
            Id = accountId,
            AccountName = _securityOptions.RootUser,
            Type = AccountType.System,
            Role = AccountRole.Root,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastLogin = DateTime.UtcNow
        };

        var credential = new AccountCredential
        {
            AccountId = accountId,
            PasswordHash = hash,
            PasswordSalt = salt,
            UpdatedAt = DateTime.UtcNow
        };

        var profile = new PersonalProfile
        {
            Id = accountId, 
            Account = rootAccount,
            DisplayName = "System Administrator",
            Description = "Built-in root account",
        };

        var settings = new PersonalSettings
        {
            Id = accountId,
            Account = rootAccount,
            Visibility = Visibility.Private
        };

        _context.Accounts.Add(rootAccount);
        _context.AccountCredentials.Add(credential);
        _context.PersonalProfiles.Add(profile);
        _context.PersonalSettings.Add(settings);

        await _context.SaveChangesAsync();
    }
}
