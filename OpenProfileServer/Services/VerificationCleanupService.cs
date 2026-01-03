using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Data;

namespace OpenProfileServer.Services;

public class VerificationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VerificationCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30);

    public VerificationCleanupService(IServiceProvider serviceProvider, ILogger<VerificationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Verification Code Cleanup Service started.");

        using var timer = new PeriodicTimer(_cleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var deletedCount = await context.VerificationCodes
                    .Where(v => v.ExpiresAt < now)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired verification codes.", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during verification code cleanup.");
            }
        }
    }
}