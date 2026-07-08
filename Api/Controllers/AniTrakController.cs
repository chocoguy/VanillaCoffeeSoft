using Api.Helpers;
using Api.SQLService;
using Api.WebDataService;
using Microsoft.AspNetCore.Mvc;
using Model.AniTrak;
using Model.AniTrak.DataTransfer;


namespace Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AniTrakController : ControllerBase
{
    private readonly IAnimeRespository _animeRepository;
    private readonly IMalWebData _malWebData;
    private readonly ILogger<AniTrakController> _logger;
    private readonly IConfiguration _configuration;

    public AniTrakController(IAnimeRespository animeRepository, ILogger<AniTrakController> logger,  IMalWebData malWebData, IConfiguration configuration)
    {
        _animeRepository = animeRepository;
        _logger = logger;
        _malWebData = malWebData;
        _configuration = configuration;
    }

    [HttpGet("Anime/{aniId}")]
    public async Task<ActionResult<Anime>> GetById(int aniId)
    {
        try
        {
            var anime = await _animeRepository.GetByIdAsync(aniId);
            return Ok(anime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }


    [HttpGet("Anime/search")]
    public async Task<ActionResult<IEnumerable<Anime>>> SearchAnime(string? q = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var anime = await _animeRepository.SearchAsync(q, page, pageSize);

            if (anime.Count() < 1)
            {
                _logger.LogInformation($"Could not find anime for query '{q}', using MAL...");
                var malAnime = await _malWebData.GetMALAnimeSearch(q);
                var translatedAnime = MalDataHelper.MalAnimeSearchToDbAnimeSearch(malAnime);
                _logger.LogInformation($"Found {translatedAnime.Count()} anime for query '{q}'");
                return Ok(translatedAnime);
                
            }
            _logger.LogInformation($"Found {anime.Count()} anime for query '{q}'");
            return Ok(anime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Anime/search/forcemal")]
    public async Task<ActionResult<IEnumerable<Anime>>> SearchAnimeForceMal(string? q = null, int page = 1, int pageSize = 20)
    {
        try
        {
                var malAnime = await _malWebData.GetMALAnimeSearch(q);
                var translatedAnime = MalDataHelper.MalAnimeSearchToDbAnimeSearch(malAnime);
                _logger.LogInformation($"Found {translatedAnime.Count()} MAL anime for query '{q}'");
                return Ok(translatedAnime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Anime/seasonal")]
    public async Task<ActionResult<IEnumerable<Anime>>> GetSeasonalAnime()
    {
        try
        {
            var now = DateTime.UtcNow;
            var year = now.Year;
            var season = MalDataHelper.ResolveSeasonName(now);

            var malSeasonal = await _malWebData.GetMALSeasonalAnime(year, season);
            var seasonalAnime = MalDataHelper.MalSeasonalAnimeToDbAnime(malSeasonal, year, season);

            _logger.LogInformation($"Found {seasonalAnime.Count} anime for {season} {year}");
            return Ok(seasonalAnime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Anime/{malId}/sync")]
    public async Task<ActionResult<int>> SyncToDB(int malId, string password)
    {
        try
        {
            var syncPassword = _configuration.GetSection("SyncPassword").Value;

            if (string.IsNullOrEmpty(syncPassword) || password != syncPassword)
            {
                _logger.LogWarning($"Unauthorized sync attempt for MAL id '{malId}'");
                return Unauthorized();
            }

            var malAnime = await _malWebData.GetMALAnimeById(malId.ToString());

            if (malAnime == null)
            {
                _logger.LogWarning($"Could not retrieve MAL id '{malId}' for sync");
                return NotFound();
            }

            var animeId = await _animeRepository.AddEditAsync(malAnime);

            if (animeId == null)
            {
                _logger.LogError($"Failed to sync MAL id '{malId}' to the database");
                return Problem();
            }

            _logger.LogInformation($"Synced MAL id '{malId}' ('{malAnime.title}') to the database as AnimeId '{animeId}'");
            return Ok(animeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Anime/{malId}/recommendations")]
    public async Task<ActionResult<IEnumerable<ParsedReccomendedAnime>>> GetRecommendations(int malId)
    {
        try
        {
            var recommendations = await _malWebData.GetMALAnimeRecommendations(malId.ToString());
            var parsed = MalDataHelper.MalRecommendationsToParsed(recommendations);
            _logger.LogInformation($"Found {parsed.Count} recommendations for MAL id '{malId}'");
            return Ok(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Anime/{malId}/related")]
    public async Task<ActionResult<IEnumerable<ParsedRelatedAnime>>> GetRelatedAnime(int malId)
    {
        try
        {
            var relatedAnime = await _malWebData.GetMALAnimeRelatedAnime(malId.ToString());
            var parsed = MalDataHelper.MalRelatedAnimeToParsed(relatedAnime);
            _logger.LogInformation($"Found {parsed.Count} related anime for MAL id '{malId}'");
            return Ok(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }

    [HttpGet("Anime/{malId}/pictures")]
    public async Task<ActionResult<IEnumerable<string>>> GetPictures(int malId)
    {
        try
        {
            var pictures = await _malWebData.GetMALAnimePictures(malId.ToString());
            var parsed = MalDataHelper.MalPicturesToStrings(pictures);
            _logger.LogInformation($"Found {parsed.Count} pictures for MAL id '{malId}'");
            return Ok(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return Problem();
        }
    }
}