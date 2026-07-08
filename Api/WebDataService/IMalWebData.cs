using Model.AniTrak.DataTransfer;

namespace Api.WebDataService;

public interface IMalWebData
{
    Task<MAL_Anime> GetMALAnimeById(string malId);
    Task<MAL_Anime_Search> GetMALAnimeSearch(string query);
    Task<MAL_Anime_Search> GetMALSeasonalAnime(int year, string season);
    Task<List<MAL_Recommendation>> GetMALAnimeRecommendations(string malId);
    Task<List<MAL_RelatedAnime>> GetMALAnimeRelatedAnime(string malId);
    Task<List<MAL_Picture>> GetMALAnimePictures(string malId);
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

    public async Task<MAL_Anime>? GetMALAnimeById(string malId)
    {
        try
        {
            var client = _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync(
                $"anime/{malId}?fields=id,title,main_picture,alternative_titles,start_date,end_date,synopsis,mean,rank,popularity,num_list_users,num_scoring_users,nsfw,created_at,updated_at,media_type,status,genres,my_list_status,num_episodes,start_season,broadcast,source,average_episode_duration,rating,pictures,background,related_anime,related_manga,recommendations,studios,statistics");
            
            MAL_Anime malAnime = await res.Content.ReadFromJsonAsync<MAL_Anime>();
            
            return malAnime;
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<MAL_Anime_Search>? GetMALAnimeSearch(string query)
    {
        try
        {
            var client =  _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync($"anime?q={query}&limit=12&fields=alternative_titles,start_date,start_season,media_type,num_episodes");
            MAL_Anime_Search malAnimeSearch = await res.Content.ReadFromJsonAsync<MAL_Anime_Search>();
            return malAnimeSearch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<MAL_Anime_Search>? GetMALSeasonalAnime(int year, string season)
    {
        try
        {
            var client = _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync($"anime/season/{year}/{season}?limit=200&fields=alternative_titles,start_date,start_season,media_type,num_episodes&sort=anime_num_list_users");
            MAL_Anime_Search malAnimeSearch = await res.Content.ReadFromJsonAsync<MAL_Anime_Search>();
            return malAnimeSearch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<List<MAL_Recommendation>>? GetMALAnimeRecommendations(string malId)
    {
        try
        {
            var client =  _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync($"anime/{malId}?fields=recommendations");
            MAL_Anime malAnime = await res.Content.ReadFromJsonAsync<MAL_Anime>();
            
            return malAnime.recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<List<MAL_RelatedAnime>>? GetMALAnimeRelatedAnime(string malId)
    {
        try
        {
            var client =  _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync($"anime/{malId}?fields=related_anime");
            MAL_Anime malAnime = await res.Content.ReadFromJsonAsync<MAL_Anime>();
            
            return malAnime.related_anime;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<List<MAL_Picture>>? GetMALAnimePictures(string malId)
    {
        try
        {
            var client =  _httpFactory.CreateClient("MalClient");
            var res = await client.GetAsync($"anime/{malId}?fields=pictures");
            MAL_Anime malAnime = await res.Content.ReadFromJsonAsync<MAL_Anime>();

            return malAnime.pictures;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }
}