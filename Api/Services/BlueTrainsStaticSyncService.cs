using Api.Gtfs;
using Api.Helpers;
using Api.SQLService;
using Api.WebDataService;

namespace Api.Services;

public sealed class BlueTrainsStaticSyncService : BackgroundService
{
    //Run at 4AM CST every day
    private static readonly TimeOnly RunAtChicago = new(4, 0);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlueTrainsStaticSyncService> _logger;

    public BlueTrainsStaticSyncService(IServiceScopeFactory scopeFactory,
        ILogger<BlueTrainsStaticSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await SyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GtfsDateTimeHelper.DelayUntilNextChicagoTime(RunAtChicago, DateTime.UtcNow);
            _logger.LogInformation("Next static schedule sync scheduled for {RunTime:u} (in {Delay})", DateTime.UtcNow + delay, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            await SyncAsync(stoppingToken);
        }
    }
    
    internal async Task SyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var metra = scope.ServiceProvider.GetRequiredService<IMetraWebData>();
            var repository = scope.ServiceProvider.GetRequiredService<IBlueTrainsStaticImportRepository>();

            var publishTime = await metra.GetStaticSchedulePublishTime(stoppingToken);
            if (publishTime == null)
            {
                _logger.LogWarning("Static sync: publish time unavailable, skipping this run");
                return;
            }
            
            var lastApplied = await repository.GetLastAppliedPublishTimeAsync();
            if (lastApplied == publishTime)
            {
                _logger.LogInformation("Static sync: schedule unchanged (published {PublishTime:u}), skipping",
                    publishTime);
                return;
            }

            var zipPath = Path.Combine(Path.GetTempPath(), $"metra-gtfs-{Guid.NewGuid():N}.zip");

            try
            {
                if (!await metra.GetStaticScheduleZip(zipPath, stoppingToken))
                {
                    _logger.LogWarning("Static sync: schedule.zip download failed, keeping current data");
                    return;
                }

                var feed = GtfsFeedReader.ReadFromZip(zipPath);
                var lookups = await repository.GetLookupsAsync();
                var model = GtfsImportModelBuilder.Build(feed, lookups,
                    GtfsDateTimeHelper.TodayInChicago(DateTime.UtcNow),
                    msg => _logger.LogWarning("Static sync: {Message}", msg));

                await repository.ImportAsync(model, publishTime.Value);

                _logger.LogInformation(
                    "Static sync applied schedule published {PublishTime:u}: {Runs} runs, {RunDates} run-dates, {Stops} stops",
                    publishTime, model.Runs.Count, model.RunDateCount, model.StopCount);
            }
            finally
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }
        }
        catch (OperationCanceledException)
        {
            // host shutting down mid-run
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Static sync run failed; keeping current data");
        }
    }
}
