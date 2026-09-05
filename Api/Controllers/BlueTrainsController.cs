using Api.SQLService;
using Microsoft.AspNetCore.Mvc;
using Model.BlueTrains.ConsumerDataTransfer;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlueTrainsController : ControllerBase
{
    private readonly IBlueTrainsRepository _blueTrainsRepository;
    private readonly ILogger<BlueTrainsController> _logger;

    public BlueTrainsController(IBlueTrainsRepository blueTrainsRepository, ILogger<BlueTrainsController> logger)
    {
        _blueTrainsRepository = blueTrainsRepository;
        _logger = logger;
    }

    
    //Service day the stop times are composed against ("yyyy-MM-dd"). Defaults to the
    //train's next scheduled date, falling back to the most recent one it ran.
    [HttpGet("Train/{trainId:int}")]
    public async Task<ActionResult<Train>> GetTrainById(int trainId, [FromQuery] DateTime? date = null)
    {
        try
        {
            var train = await _blueTrainsRepository.GetTrainAsync(trainId, ToServiceDate(date));

            if (train == null)
                return NotFound();

            return Ok(train);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Train/{lineId:int}/{day}")]
    public async Task<ActionResult<List<Train>>> GetAllTrainsByDate(DateTime day, int lineId)
    {
        try
        {
            var trains = await _blueTrainsRepository.GetTrainsByLineAndDateAsync(lineId, ToServiceDate(day)!.Value);

            // Null means the line itself is unknown; an empty list means it ran nothing that day.
            if (trains == null)
                return NotFound();

            _logger.LogInformation($"Found {trains.Count} trains on line {lineId} for {day:yyyy-MM-dd}");
            return Ok(trains);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("TrainStation/{trainStationId:int}")]
    public async Task<ActionResult<TrainStation>> GetTrainStationById(int trainStationId)
    {
        try
        {
            var station = await _blueTrainsRepository.GetStationAsync(trainStationId);

            if (station == null)
                return NotFound();

            return Ok(station);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("TrainStation/Line/{lineId:int}")]
    public async Task<ActionResult<List<TrainStation>>> GetTrainStationsByLineId(int lineId)
    {
        try
        {
            var stations = await _blueTrainsRepository.GetStationsByLineAsync(lineId);

            if (stations == null)
                return NotFound();

            return Ok(stations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }


    [HttpGet("Line/{lineId:int}")]
    public async Task<ActionResult<TrainLine>> GetLineById(int lineId)
    {
        try
        {
            var line = await _blueTrainsRepository.GetLineAsync(lineId);

            if (line == null)
                return NotFound();

            return Ok(line);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Line/All")]
    public async Task<ActionResult<List<TrainLine>>> GetAllLines()
    {
        try
        {
            return Ok(await _blueTrainsRepository.GetAllLinesAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }


    [HttpGet("Advisory/{advisoryId:int}")]
    public async Task<ActionResult<TrainAdvisory>> GetAdvisoryById(int advisoryId)
    {
        try
        {
            var advisory = await _blueTrainsRepository.GetAdvisoryAsync(advisoryId);

            if (advisory == null)
                return NotFound();

            return Ok(advisory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Advisory/Line/{lineId:int}")]
    public async Task<ActionResult<List<TrainAdvisory>>> GetAdvisoriesByLineId(int lineId)
    {
        try
        {
            var advisories = await _blueTrainsRepository.GetAdvisoriesByLineAsync(lineId);

            // Null means the line itself is unknown; an empty list just means nothing is posted.
            if (advisories == null)
                return NotFound();

            return Ok(advisories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }
    
    // Matched by scanning advisory text for the run number ("train 145", "Train #2156",
    // "#1296"), since the feed has no structured trip reference on alerts.
    [HttpGet("Advisory/TrainNumber/{trainNumber:int}")]
    public async Task<ActionResult<List<TrainAdvisory>>> GetAdvisoriesByTrainNumber(int trainNumber)
    {
        try
        {
            var advisories = await _blueTrainsRepository.GetAdvisoriesByTrainNumberAsync(trainNumber);

            // Null means no run carries that number; an empty list means none is posted about it.
            if (advisories == null)
                return NotFound();

            return Ok(advisories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Advisory/System")]
    public async Task<ActionResult<List<TrainAdvisory>>> GetSystemwideAdvisories()
    {
        try
        {
            return Ok(await _blueTrainsRepository.GetSystemwideAdvisoriesAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    // Route/query dates name a service day, never an instant, so the clock part is dropped
    // before it can shift the day across a time zone.
    private static DateOnly? ToServiceDate(DateTime? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value) : null;
}
