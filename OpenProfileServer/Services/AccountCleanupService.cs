using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Constants;
using OpenProfileServer.Data;
using OpenProfileServer.Interfaces;
using OpenProfileServer.Models.Entities;
using OpenProfileServer.Models.Enums;

namespace OpenProfileServer.Services;

public class AccountCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AccountCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public AccountCleanupService(IServiceProvider serviceProvider, ILogger<AccountCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Account Cleanup Service started.");

        using var timer = new PeriodicTimer(_cleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var settingService = scope.ServiceProvider.GetRequiredService<ISystemSettingService>();

                var cooldownDays = await settingService.GetIntAsync(SystemSettingKeys.AccountDeletionCooldownDays, 30);
                var cutoffDate = DateTime.UtcNow.AddDays(-cooldownDays);

                var deletedAccounts = await ExecuteDeleteAsync(context, cutoffDate, stoppingToken);

                if (deletedAccounts > 0)
                {
                    _logger.LogInformation("Deleted {Count} accounts past cooldown period.", deletedAccounts);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during account cleanup.");
            }
        }
    }

    private async Task<int> ExecuteDeleteAsync(ApplicationDbContext context, DateTime cutoffDate, CancellationToken token)
    {
        var accountsToDelete = await context.Accounts
            .Where(a => a.Status == AccountStatus.PendingDeletion && a.UpdatedAt < cutoffDate)
            .Include(a => a.Profile)
            .Include(a => a.Settings)
            .Include(a => a.Credential)
            .Include(a => a.Security)
            .Include(a => a.Memberships)
            .Include(a => a.Followers)
            .Include(a => a.Following)
            .Include(a => a.BlockedUsers)
            .Include(a => a.BlockedBy)
            .Include(a => a.Emails)
            .Include(a => a.RefreshTokens)
            .ToListAsync(token);

        if (accountsToDelete.Count == 0) return 0;

        foreach (var account in accountsToDelete)
        {
            await DeleteAccountAsync(context, account);
        }

        await context.SaveChangesAsync(token);
        return accountsToDelete.Count;
    }

    private async Task DeleteAccountAsync(ApplicationDbContext context, Account account)
    {
        context.AccountFollowers.RemoveRange(account.Followers);
        context.AccountFollowers.RemoveRange(account.Following);
        context.AccountBlocks.RemoveRange(account.BlockedUsers);
        context.AccountBlocks.RemoveRange(account.BlockedBy);
        context.OrganizationMembers.RemoveRange(account.Memberships);
        context.AccountEmails.RemoveRange(account.Emails);
        context.RefreshTokens.RemoveRange(account.RefreshTokens);

        if (account.Profile != null)
        {
            context.Profiles.Remove(account.Profile);
        }

        if (account.Settings != null)
        {
            context.AccountSettings.Remove(account.Settings);
        }

        if (account.Credential != null)
        {
            context.AccountCredentials.Remove(account.Credential);
        }

        if (account.Security != null)
        {
            context.AccountSecurities.Remove(account.Security);
        }

        context.Accounts.Remove(account);
    }
}
