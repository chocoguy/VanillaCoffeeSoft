using Api.SQLService;
using Api.WebDataService;

namespace Api.Services;

/// <summary>
/// Runs every day at 02:00 UTC and re-syncs any anime whose LastSynced
/// is more than 10 days old, pulling fresh data from MAL.
/// </summary>
public sealed class StaleRecordSyncService : BackgroundService
{
    private static readonly TimeOnly RunAt = new(2, 0);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromDays(10);
    private static readonly TimeSpan DelayBetweenCalls = TimeSpan.FromSeconds(20);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleRecordSyncService> _logger;

    public StaleRecordSyncService(IServiceScopeFactory scopeFactory, ILogger<StaleRecordSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun(DateTime.UtcNow);
            _logger.LogInformation("Next stale-record sync scheduled for {RunTime:u} (in {Delay})",
                DateTime.UtcNow + delay, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await SyncStaleRecordsAsync(stoppingToken);
        }
    }

    private static TimeSpan DelayUntilNextRun(DateTime nowUtc)
    {
        var todayRun = nowUtc.Date + RunAt.ToTimeSpan();
        var nextRun = nowUtc < todayRun ? todayRun : todayRun.AddDays(1);
        return nextRun - nowUtc;
    }

    private async Task SyncStaleRecordsAsync(CancellationToken stoppingToken)
    {
        try
        {
            // BackgroundService is a singleton; the repository and MAL client
            // are scoped, so resolve them from a fresh scope per run.
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAnimeRespository>();
            var malWebData = scope.ServiceProvider.GetRequiredService<IMalWebData>();

            var cutoff = DateTime.UtcNow - StaleAfter;
            var staleMalIds = (await repository.GetStaleMalIdsAsync(cutoff)).ToList();

            if (staleMalIds.Count == 0)
            {
                _logger.LogInformation("Stale-record sync: nothing to update");
                return;
            }

            _logger.LogInformation("Stale-record sync: {Count} record(s) older than {Days} days",
                staleMalIds.Count, StaleAfter.TotalDays);

            var updated = 0;
            var failed = 0;

            foreach (var malId in staleMalIds)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var malAnime = await malWebData.GetMALAnimeById(malId.ToString());

                if (malAnime == null || await repository.AddEditAsync(malAnime) == null)
                {
                    _logger.LogWarning("Stale-record sync: failed to update MAL id '{MalId}'", malId);
                    failed++;
                }
                else
                {
                    updated++;
                }

                await Task.Delay(DelayBetweenCalls, stoppingToken);
            }

            _logger.LogInformation("Stale-record sync finished: {Updated} updated, {Failed} failed", updated, failed);
        }
        catch (TaskCanceledException)
        {
            // host shutting down mid-run
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stale-record sync run failed");
        }
    }
}
