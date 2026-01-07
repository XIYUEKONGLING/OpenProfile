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
        var accountIds = await context.Accounts
            .AsNoTracking()
            .Where(a => a.Status == AccountStatus.PendingDeletion && a.DeletedAt < cutoffDate)
            .Select(a => a.Id)
            .ToListAsync(token);

        if (accountIds.Count == 0) return 0;

        int totalDeleted = 0;
        foreach (var accountId in accountIds)
        {
            var deleted = await DeleteAccountByIdAsync(context, accountId, token);
            if (deleted) totalDeleted++;
        }

        return totalDeleted;
    }

    private async Task<bool> DeleteAccountByIdAsync(ApplicationDbContext context, Guid accountId, CancellationToken token)
    {
        using var transaction = await context.Database.BeginTransactionAsync(token);
        try
        {
            await context.AccountFollowers
                .Where(f => f.FollowerId == accountId || f.FollowingId == accountId)
                .ExecuteDeleteAsync(token);

            await context.AccountBlocks
                .Where(b => b.BlockerId == accountId || b.BlockedId == accountId)
                .ExecuteDeleteAsync(token);

            await context.OrganizationMembers
                .Where(m => m.AccountId == accountId)
                .ExecuteDeleteAsync(token);

            await context.AccountEmails
                .Where(e => e.AccountId == accountId)
                .ExecuteDeleteAsync(token);

            await context.RefreshTokens
                .Where(t => t.AccountId == accountId)
                .ExecuteDeleteAsync(token);

            await context.Profiles
                .Where(p => p.Id == accountId)
                .ExecuteDeleteAsync(token);

            await context.AccountSettings
                .Where(s => s.Id == accountId)
                .ExecuteDeleteAsync(token);

            await context.AccountCredentials
                .Where(c => c.AccountId == accountId)
                .ExecuteDeleteAsync(token);

            await context.AccountSecurities
                .Where(s => s.AccountId == accountId)
                .ExecuteDeleteAsync(token);

            var deleted = await context.Accounts
                .Where(a => a.Id == accountId)
                .ExecuteDeleteAsync(token);

            await transaction.CommitAsync(token);
            return deleted > 0;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(token);
            throw;
        }
    }
}
