using Api.Helpers;
using Model.BlueTrains;
using FeedMessage = TransitRealtime.FeedMessage;

namespace Api.WebDataService;

public interface IMetraWebData
{
    Task<bool> GetStaticScheduleZip(string destinationPath, CancellationToken ct = default);
    Task<DateTime?> GetStaticSchedulePublishTime(CancellationToken ct = default);
    Task<FeedMessage?> GetAlerts(CancellationToken ct = default);
    Task<TripUpdate>? GetTripUpdates();
    Task<RunPosition>? GetRunPosition(string tripId);

}


public class MetraWebData : IMetraWebData
{
    private readonly ILogger<MetraWebData> _logger;
    private readonly IHttpClientFactory _httpFactory;


    public MetraWebData(ILogger<MetraWebData> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpFactory = httpClientFactory;
    }

    public async Task<bool> GetStaticScheduleZip(string destinationPath, CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient("MetraClientStatic");
            
            using var res = await client.GetAsync("schedule.zip", HttpCompletionOption.ResponseHeadersRead, ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Metra schedule.zip returned {StatusCode}", res.StatusCode);
                return false;
            }

            await using var file = File.Create(destinationPath);
            await res.Content.CopyToAsync(file, ct);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async Task<DateTime?> GetStaticSchedulePublishTime(CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient("MetraClientStatic");
            using var res = await client.GetAsync("published.txt", ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Metra published.txt returned {StatusCode}", res.StatusCode);
                return null;
            }

            var body = await res.Content.ReadAsStringAsync(ct);
            var publishTime = GtfsDateTimeHelper.ParsePublishedTimestamp(body);

            if (publishTime == null)
                _logger.LogWarning("Metra published.txt was unparseable: '{Body}'", body.Trim());

            return publishTime;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<FeedMessage?> GetAlerts(CancellationToken ct = default)
    {
        try
        {
            var client = _httpFactory.CreateClient("MetraClientRealTime");
            using var res = await client.GetAsync("alerts", HttpCompletionOption.ResponseHeadersRead, ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("M {StatusCode}", res.StatusCode);
                return null;
            }

            await using var body = await res.Content.ReadAsStreamAsync(ct);
            return FeedMessage.Parser.ParseFrom(body);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public Task<TripUpdate>? GetTripUpdates()
    {
        throw new NotImplementedException();
    }

    public Task<RunPosition>? GetRunPosition(string tripId)
    {
        throw new NotImplementedException();
    }
}