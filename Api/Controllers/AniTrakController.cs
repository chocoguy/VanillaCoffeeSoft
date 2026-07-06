using Api.Helpers;
using Api.SQLService;
using Api.WebDataService;
using Microsoft.AspNetCore.Mvc;
using Model.AniTrak;


namespace Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AniTrakController : ControllerBase
{
    private readonly IAnimeRespository _animeRepository;
    private readonly IMalWebData _malWebData;
    private readonly ILogger<AniTrakController> _logger;

    public AniTrakController(IAnimeRespository animeRepository, ILogger<AniTrakController> logger,  IMalWebData malWebData)
    {
        _animeRepository = animeRepository;
        _logger = logger;
        _malWebData = malWebData;
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

    [HttpGet("/Anime/search/forcemal")]
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
    
    
    
}