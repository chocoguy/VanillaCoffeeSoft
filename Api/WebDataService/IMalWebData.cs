using Model.AniTrak.DataTransfer;

namespace Api.WebDataService;

public interface IMalWebData
{
    Task<MAL_Anime?> GetMALAnimeById(string malId);
    Task<MAL_Anime_Search?> GetMALAnimeSearch(string? query);
    Task<MAL_Anime_Search?> GetMALSeasonalAnime(int year, string season);
    Task<List<MAL_Recommendation>?> GetMALAnimeRecommendations(string malId);
    Task<List<MAL_RelatedAnime>?> GetMALAnimeRelatedAnime(string malId); 
    Task<List<MAL_Picture>?> GetMALAnimePictures(string malId);
}

public class MalWebData : IMalWebData
{
    private readonly ILogger<MalWebData> _logger;
    private readonly IHttpClientFactory _httpFactory;


    public MalWebData(ILogger<MalWebData> logger, IHttpClientFactory httpFactory)
    {
        _logger = logger;
        _httpFactory = httpFactory;
    }

    public async Task<MAL_Anime?> GetMALAnimeById(string malId)
    {
        return await GetMalAnimeFields(malId,
            "id,title,main_picture,alternative_titles,start_date,end_date,synopsis,mean,rank,popularity,num_list_users,num_scoring_users,nsfw,created_at,updated_at,media_type,status,genres,my_list_status,num_episodes,start_season,broadcast,source,average_episode_duration,rating,pictures,background,related_anime,related_manga,recommendations,studios,statistics");
    }

    public async Task<MAL_Anime_Search?> GetMALAnimeSearch(string? query)
    {
        try
        {
            var client = _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync(
                $"anime?q={Uri.EscapeDataString(query ?? string.Empty)}&limit=12&fields=alternative_titles,start_date,start_season,media_type,num_episodes");

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("MAL search for '{Query}' returned {StatusCode}", query, res.StatusCode);
                return null;
            }

            return await res.Content.ReadFromJsonAsync<MAL_Anime_Search>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<MAL_Anime_Search?> GetMALSeasonalAnime(int year, string season)
    {
        try
        {
            var client = _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync(
                $"anime/season/{year}/{season}?limit=200&fields=alternative_titles,start_date,start_season,media_type,num_episodes&sort=anime_num_list_users");

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("MAL seasonal for {Season} {Year} returned {StatusCode}", season, year, res.StatusCode);
                return null;
            }

            return await res.Content.ReadFromJsonAsync<MAL_Anime_Search>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<List<MAL_Recommendation>?> GetMALAnimeRecommendations(string malId)
    {
        var malAnime = await GetMalAnimeFields(malId, "recommendations");
        return malAnime?.recommendations;
    }

    public async Task<List<MAL_RelatedAnime>?> GetMALAnimeRelatedAnime(string malId)
    {
        var malAnime = await GetMalAnimeFields(malId, "related_anime");
        return malAnime?.related_anime;
    }

    public async Task<List<MAL_Picture>?> GetMALAnimePictures(string malId)
    {
        var malAnime = await GetMalAnimeFields(malId, "pictures");
        return malAnime?.pictures;
    }

    private async Task<MAL_Anime?> GetMalAnimeFields(string malId, string fields)
    {
        try
        {
            var client = _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync($"anime/{Uri.EscapeDataString(malId)}?fields={fields}");

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("MAL anime id '{MalId}' returned {StatusCode}", malId, res.StatusCode);
                return null;
            }

            return await res.Content.ReadFromJsonAsync<MAL_Anime>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }
}
