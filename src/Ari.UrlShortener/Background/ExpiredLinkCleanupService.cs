using Ari.UrlShortener.Data;
using Ari.UrlShortener.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ari.UrlShortener.Background;

/// <summary>
/// Periodically marks expired links as inactive. The redirect path already checks expiry
/// live, so this is housekeeping that keeps the data consistent and the active set small.
/// </summary>
public sealed class ExpiredLinkCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ShortLinkOptions _options;
    private readonly ILogger<ExpiredLinkCleanupService> _logger;

    public ExpiredLinkCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<ShortLinkOptions> options,
        ILogger<ExpiredLinkCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, _options.CleanupIntervalHours));

        // Run once at startup, then on the configured interval.
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTime.UtcNow;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var affected = await db.ShortLinks
                .Where(x => x.IsActive && x.ExpiresAtUtc <= now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(x => x.IsActive, false),
                    cancellationToken);

            if (affected > 0)
            {
                _logger.LogInformation("Cleanup marked {Count} expired link(s) as inactive.", affected);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutting down — ignore.
        }
        catch (Exception ex)
        {
            // Never let a transient failure tear down the background loop.
            _logger.LogError(ex, "Expired-link cleanup pass failed.");
        }
    }
}
