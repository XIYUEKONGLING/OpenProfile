using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Entities.Profiles;
using OpenProfileServer.Models.Entities.Settings;
using OpenProfileServer.Models.Enums;
using OpenProfileServer.Models.ValueObjects;
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
        await SeedSiteMetadataAsync();
        await SeedSystemSettingsAsync();
        await SeedRootAccountAsync();
    }

    private async Task SeedSiteMetadataAsync()
    {
        if (await _context.SiteMetadata.AnyAsync()) return;

        var defaultMetadata = new SiteMetadata
        {
            SiteName = "OpenProfile",
            SiteDescription = "Open source identity management platform.",
            Copyright = $"\u00a9 {DateTime.UtcNow.Year} OpenProfile Team",
            ContactEmail = "admin@localhost",
            Logo = new Asset
            {
                Type = AssetType.Text,
                Value = "\U0001F464",
                Tag = "Default Site Logo"
            },
            Favicon = new Asset
            {
                Type = AssetType.Text,
                Value = "\U0001F194",
                Tag = "Default Favicon"
            },
            UpdatedAt = DateTime.UtcNow
        };

        _context.SiteMetadata.Add(defaultMetadata);
        await _context.SaveChangesAsync();
    }

    private async Task SeedSystemSettingsAsync()
    {
        var defaults = new Dictionary<string, (string Value, string Type, string Desc)>
        {
            // Policies (Runtime Logic)
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
                SystemSettingKeys.DefaultStorageLimit,
                ("104857600", "number", "Default storage quota in bytes (100MB).")
            },
            { 
                SystemSettingKeys.EmailAddRequiresVerification, 
                ("true", "boolean", "Requires verification code when adding a new email to an existing account.") 
            },
            
            // Email Templates
            {
                SystemSettingKeys.EmailVerificationSubject,
                ("Verify your OpenProfile account", "string", "Subject line for verification emails.")
            },
            {
                SystemSettingKeys.EmailVerificationBody,
                ("<h3>Welcome to OpenProfile, {Username}!</h3><p>Your verification code is: <strong>{Code}</strong></p><p>This code will expire in 15 minutes.</p>", "html", "HTML body for verification emails. Variables: {Username}, {Code}.")
            },
            {
                SystemSettingKeys.EmailPasswordResetSubject,
                ("Reset your password", "string", "Subject line for password reset.")
            },
            {
                SystemSettingKeys.EmailPasswordResetBody,
                ("<p>You requested a password reset. Use this code: <strong>{Code}</strong></p>", "html", "HTML body for password reset. Variables: {Username}, {Code}.")
            },
            
            // Asset Limits
            { 
                SystemSettingKeys.MaxAssetSizeBytes, 
                ("5242880", "number", "Maximum size for standard assets like avatars and logos (Default 5MB).") 
            },
            { 
                SystemSettingKeys.MaxGalleryAssetSizeBytes, 
                ("104857600", "number", "Maximum size for gallery items (Default 10MB).") 
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
