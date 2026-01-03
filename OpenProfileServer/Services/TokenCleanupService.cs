using Microsoft.EntityFrameworkCore;
using OpenProfileServer.Data;

namespace OpenProfileServer.Services;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);

    public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Cleanup Service started.");

        using var timer = new PeriodicTimer(_cleanupInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var now = DateTime.UtcNow;
                var deletedCount = await context.RefreshTokens
                    .Where(t => t.ExpiresAt < now)
                    .ExecuteDeleteAsync(stoppingToken);

                if (deletedCount > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} expired refresh tokens.", deletedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token cleanup.");
            }
        }
    }
}