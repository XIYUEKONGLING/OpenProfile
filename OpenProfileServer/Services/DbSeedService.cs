using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenProfileServer.Configuration;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Entities.Auth;
using OpenProfileServer.Models.Entities.Base;
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
        // Check if critical settings exist, if not, create them
        if (!await _context.SystemSettings.AnyAsync(s => s.Key == SystemSettingKeys.AllowRegistration))
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                Key = SystemSettingKeys.AllowRegistration,
                Value = "true",
                ValueType = "boolean",
                Description = "Allows new users to register."
            });
        }

        if (!await _context.SystemSettings.AnyAsync(s => s.Key == SystemSettingKeys.MaintenanceMode))
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                Key = SystemSettingKeys.MaintenanceMode,
                Value = "false",
                ValueType = "boolean",
                Description = "Puts the server in maintenance mode."
            });
        }
        
        await _context.SaveChangesAsync();
    }

    private async Task SeedRootAccountAsync()
    {
        // Check if any ROOT account exists
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

        // Root needs a profile to function in UI
        var profile = new PersonalProfile
        {
            Id = accountId, // Shared Key
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
