using Api.Gtfs;
using Api.SQLService;
using Api.WebDataService;

namespace Api.Services;

public sealed class BlueTrainsRealtimeSyncService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlueTrainsRealtimeSyncService> _logger;


    public BlueTrainsRealtimeSyncService(IServiceScopeFactory scopeFactory, ILogger<BlueTrainsRealtimeSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    //runs every minute
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        await SyncAlerts(stoppingToken);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SyncAlerts(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // host shutting down
        }
    }

    private async Task SyncAlerts(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var metra = scope.ServiceProvider.GetRequiredService<IMetraWebData>();
            var repository = scope.ServiceProvider.GetRequiredService<IBlueTrainsRealtimeRepository>();

            var feed = await metra.GetAlerts(stoppingToken);
            
            if (feed == null)
            {
                _logger.LogWarning("Alert sync: feed unavailable, keeping current alerts");
                return;
            }

            var observedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var alerts = GtfsAlertParser.Parse(feed, observedAt, msg => _logger.LogWarning("Alert sync: {Message}", msg));
            var result = await repository.SyncAlertsAsync(alerts, observedAt);

            _logger.LogInformation(
                "Alert sync applied feed stamped {FeedTimestamp}: {Applied} alerts live, {Cleared} cleared",
                feed.Header.Timestamp, result.Applied, result.Cleared);
        }
        catch (OperationCanceledException)
        {
            // host shutting down mid-run
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alert sync run failed; keeping current alerts");
        }
    }
}
