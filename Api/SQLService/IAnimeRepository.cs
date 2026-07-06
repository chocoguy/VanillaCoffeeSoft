using Dapper;
using System.Data;
using Model.AniTrak;
using System.Collections.Generic;
using Model.AniTrak.DataTransfer;

namespace Api.SQLService;

//QueryFirstOrDefault
//QuerySingleOrDefault
//QueryAsync
//

public interface IAnimeRespository
{
    Task<Anime?> GetByIdAsync(int id);
    Task<IEnumerable<Anime>?> GetAllAsync();
    Task<IEnumerable<Anime>?> SearchAsync(string? query = null, int page = 1, int pageSize = 20);
    Task<bool> AddEditAsync(MAL_Anime malAnime);
    Task<bool> CheckIfExists(int id);
}

public class AnimeRepository : IAnimeRespository
{

    private readonly IDbConnectionFactory _dbConnection;
    private readonly ILogger<AnimeRepository> _logger;

    public AnimeRepository(IDbConnectionFactory dbConnection, ILogger<AnimeRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<Anime?> GetByIdAsync(int id)
    {
        try
        {
      using var connection = _dbConnection.CreateConnection();
        connection.Open();

        var sql = @"
SELECT 
    a.*,
    s.SeasonId, s.Name,
    ad.AirDayId, ad.Name,
    mt.MediaTypeId, mt.Name,
    os.OriginalSourceId, os.Name,
    n.NsfwId, n.Name
FROM Anime a
LEFT JOIN Season s ON a.SeasonKey = s.SeasonId
LEFT JOIN AirDay ad ON a.AirDayKey = ad.AirDayId
LEFT JOIN MediaType mt ON a.MediaTypeKey = mt.MediaTypeId
LEFT JOIN OriginalSource os ON a.OriginalSourceKey = os.OriginalSourceId
LEFT JOIN Nsfw n ON a.NsfwKey = n.NsfwId
WHERE a.AnimeId = @Id;";

        var anime = (await connection.QueryAsync<Anime, Season, AirDay, MediaType, OriginalSource, Nsfw, Anime>(
            sql,
            (animeItem, season, airDay, mediaType, originalSource, nsfw) =>
            {
                if (season != null && season.SeasonId != 0)
                    animeItem.Season = season;
                if (airDay != null && airDay.AirDayId != 0)
                    animeItem.AirDay = airDay;
                if (mediaType != null && mediaType.MediaTypeId != 0)
                    animeItem.MediaType = mediaType;
                if (originalSource != null && originalSource.OriginalSourceId != 0)
                    animeItem.OriginalSource = originalSource;
                if (nsfw != null && nsfw.NsfwId != 0)
                    animeItem.Nsfw = nsfw;
                return animeItem;
            },
            new { Id = id },
            splitOn: "SeasonId,AirDayId,MediaTypeId,OriginalSourceId,NsfwId"
        )).FirstOrDefault();

        if (anime == null) return null;

        // Fetch Tags (many-to-many via junction table) — unchanged logic, just cleaned up
        const string tagsSql = @"
    SELECT t.Name 
    FROM TagAnime ta 
    INNER JOIN Tag t ON t.TagId = ta.TagKey 
    WHERE ta.AnimeKey = @AnimeId";

        anime.Tags = (await connection.QueryAsync<string>(tagsSql, new { AnimeId = id })).ToList();

        // Fetch Studios (many-to-many via junction table)
        const string studiosSql = @"
    SELECT st.Name 
    FROM StudioAnime sa 
    INNER JOIN Studio st ON st.StudioId = sa.StudioKey 
    WHERE sa.AnimeKey = @AnimeId";

        anime.Studios = (await connection.QueryAsync<string>(studiosSql, new { AnimeId = id })).ToList();

        return anime;


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<IEnumerable<Anime>?> GetAllAsync()
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();
            return await connection.QueryAsync<Anime>("SELECT * FROM Anime");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<IEnumerable<Anime>?> SearchAsync(string? query = null, int page = 1, int pageSize = 20)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();
            
            var parameters = new DynamicParameters();
            parameters.Add("PageSize", pageSize);
            parameters.Add("Offset", (page - 1) * pageSize);

            string whereClause = "";
            string orderBy = "ORDER BY a.Title";

            if (!string.IsNullOrWhiteSpace(query))
            {
                whereClause = "WHERE f.AnimeFTSReaL MATCH @SearchQuery";
                parameters.Add("SearchQuery", query);
                orderBy = "ORDER BY f.rank DESC, a.Title";
            }
            
            var sql = $@"
SELECT
    a.animeid, a.title, a.titleshort, a.titleromanized, a.titlekana,
    a.year, a.synopsis, a.poster, a.malscore,
    s.Name AS SeasonName,
    mt.Name AS MediaTypeName
FROM Anime a
         INNER JOIN Season s ON a.SeasonKey = s.SeasonId
         INNER JOIN MediaType mt ON a.MediaTypeKey = mt.MediaTypeId
         INNER JOIN AnimeFTSReal f ON a.AnimeId = f.rowid
            {whereClause}
            {orderBy}
LIMIT @PageSize OFFSET @Offset;";
            
            return await connection.QueryAsync<Anime>(sql, parameters);

            
            
            
            
            
            //WHERE f.AnimeFTSReal  MATCH 'LycoReco'
            //ORDER BY f.rank DESC, a.Title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<bool> AddEditAsync(MAL_Anime malAnime)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();
            
            //await connection.ExecuteAsync("DELETE FROM AnimeFTS WHERE rowid = @Id;", new { Id = animeId });

            int animeId = 0;
            
            var sql =
                @"SELECT * FROM Anime WHERE Title = @Title;";
            var anime = await connection.QueryFirstOrDefaultAsync<Anime>(sql, new { Title = animeId });


            if (anime != null)
            {
                
            }
            else
            {
                
            }



            await connection.ExecuteAsync(
                @"INSERT INTO AnimeFTSReal(rowid, Title, TitleShort, TitleRomanized, TitleKana)
        SELECT AnimeId, Title, TitleShort, TitleRomanized, TitleKana
        FROM Anime 
        WHERE AnimeId = @Id;", new { Id = animeId });
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async Task<bool> CheckIfExists(int id)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();



            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }
}