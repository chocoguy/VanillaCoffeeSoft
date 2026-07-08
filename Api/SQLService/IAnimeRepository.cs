using Api.Helpers;
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
    Task<int?> AddEditAsync(MAL_Anime malAnime);
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
    a.animeid, a.MalId, a.title, a.titleshort, a.titleromanized, a.titlekana,
    a.year, a.EpisodeCount, a.synopsis, a.poster, a.malscore,
    s.SeasonId, s.Name,
    mt.MediaTypeId, mt.Name
FROM Anime a
         INNER JOIN Season s ON a.SeasonKey = s.SeasonId
         INNER JOIN MediaType mt ON a.MediaTypeKey = mt.MediaTypeId
         INNER JOIN AnimeFTSReal f ON a.AnimeId = f.rowid
            {whereClause}
            {orderBy}
LIMIT @PageSize OFFSET @Offset;";

            return await connection.QueryAsync<Anime, Season, MediaType, Anime>(
                sql,
                (animeItem, season, mediaType) =>
                {
                    if (season != null && season.SeasonId != 0)
                        animeItem.Season = season;
                    if (mediaType != null && mediaType.MediaTypeId != 0)
                        animeItem.MediaType = mediaType;
                    return animeItem;
                },
                parameters,
                splitOn: "SeasonId,MediaTypeId");

            
            
            
            
            
            //WHERE f.AnimeFTSReal  MATCH 'LycoReco'
            //ORDER BY f.rank DESC, a.Title;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<int?> AddEditAsync(MAL_Anime malAnime)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var now = DateTime.UtcNow;

            AniTrakDictionary.D_Season.TryGetValue(malAnime.start_season?.season ?? "", out var seasonKey);
            AniTrakDictionary.D_AirDay.TryGetValue(malAnime.broadcast?.day_of_the_week ?? "", out var airDayKey);
            AniTrakDictionary.D_MediaType.TryGetValue(malAnime.media_type ?? "", out var mediaTypeKey);
            AniTrakDictionary.D_OriginalSource.TryGetValue(malAnime.source ?? "", out var originalSourceKey);
            AniTrakDictionary.D_Nsfw.TryGetValue(malAnime.nsfw ?? "", out var nsfwKey);

            var parameters = new
            {
                MalId = malAnime.id,
                Title = malAnime.title,
                TitleShort = ResolveTitleShort(malAnime),
                TitleRomanized = malAnime.alternative_titles?.en,
                TitleKana = malAnime.alternative_titles?.ja,
                SeasonKey = seasonKey,
                AirDayKey = airDayKey,
                MediaTypeKey = mediaTypeKey,
                OriginalSourceKey = originalSourceKey,
                NsfwKey = nsfwKey,
                Year = malAnime.start_season?.year ?? 0,
                EpisodeCount = malAnime.num_episodes,
                AirTime = malAnime.broadcast?.start_time,
                OnAir = DateTimeHelper.ParseFlexible(malAnime.start_date),
                OffAir = DateTimeHelper.ParseFlexible(malAnime.end_date),
                Synopsis = malAnime.synopsis,
                Reception = malAnime.status,
                MalScore = malAnime.mean,
                MalRank = malAnime.rank,
                MalWatching = ParseMalCount(malAnime.statistics?.status?.watching),
                MalCompleted = ParseMalCount(malAnime.statistics?.status?.completed),
                MalOnHold = ParseMalCount(malAnime.statistics?.status?.on_hold),
                MalDropped = ParseMalCount(malAnime.statistics?.status?.dropped),
                MalPlanToWatch = ParseMalCount(malAnime.statistics?.status?.plan_to_watch),
                Poster = malAnime.main_picture?.large ?? malAnime.main_picture?.medium,
                EpisodeLength = malAnime.average_episode_duration,
                LastSynced = now,
                LastModified = now
            };

            int animeId;
            var exists = await CheckIfExists(malAnime.id);

            if (exists)
            {
                animeId = await connection.QuerySingleAsync<int>(
                    "SELECT AnimeId FROM Anime WHERE MalId = @MalId;", new { MalId = malAnime.id }, transaction);

                // AnimeFTSReal is an external-content FTS5 table: the row must be
                // removed from the index while it still matches the Anime content,
                // i.e. BEFORE the UPDATE below, or FTS5 reports index corruption.
                await connection.ExecuteAsync(
                    "DELETE FROM AnimeFTSReal WHERE rowid = @Id;", new { Id = animeId }, transaction);

                const string updateSql = @"
UPDATE Anime SET
    Title = @Title, TitleShort = @TitleShort, TitleRomanized = @TitleRomanized, TitleKana = @TitleKana,
    SeasonKey = @SeasonKey, AirDayKey = @AirDayKey, MediaTypeKey = @MediaTypeKey, OriginalSourceKey = @OriginalSourceKey, NsfwKey = @NsfwKey,
    Year = @Year, EpisodeCount = @EpisodeCount, AirTime = @AirTime, OnAir = @OnAir, OffAir = @OffAir,
    Synopsis = @Synopsis, Reception = @Reception, MalScore = @MalScore, MalRank = @MalRank,
    MalWatching = @MalWatching, MalCompleted = @MalCompleted, MalOnHold = @MalOnHold, MalDropped = @MalDropped, MalPlanToWatch = @MalPlanToWatch,
    Poster = @Poster, EpisodeLength = @EpisodeLength, LastSynced = @LastSynced, LastModified = @LastModified
WHERE MalId = @MalId;";

                await connection.ExecuteAsync(updateSql, parameters, transaction);
            }
            else
            {
                const string insertSql = @"
INSERT INTO Anime (
    Title, TitleShort, TitleRomanized, TitleKana, SeasonKey, AirDayKey, MediaTypeKey, OriginalSourceKey, NsfwKey,
    Year, EpisodeCount, AirTime, OnAir, OffAir, Synopsis, Reception, MalScore, MalRank,
    MalWatching, MalCompleted, MalOnHold, MalDropped, MalPlanToWatch, Poster, EpisodeLength, LastSynced, LastModified, MalId
) VALUES (
    @Title, @TitleShort, @TitleRomanized, @TitleKana, @SeasonKey, @AirDayKey, @MediaTypeKey, @OriginalSourceKey, @NsfwKey,
    @Year, @EpisodeCount, @AirTime, @OnAir, @OffAir, @Synopsis, @Reception, @MalScore, @MalRank,
    @MalWatching, @MalCompleted, @MalOnHold, @MalDropped, @MalPlanToWatch, @Poster, @EpisodeLength, @LastSynced, @LastModified, @MalId
);
SELECT last_insert_rowid();";

                animeId = await connection.QuerySingleAsync<int>(insertSql, parameters, transaction);
            }

            await SyncStudiosAsync(connection, transaction, animeId, malAnime.studios);
            await SyncTagsAsync(connection, transaction, animeId, malAnime.genres);

            await connection.ExecuteAsync(
                @"INSERT INTO AnimeFTSReal(rowid, Title, TitleShort, TitleRomanized, TitleKana)
        SELECT AnimeId, Title, TitleShort, TitleRomanized, TitleKana
        FROM Anime
        WHERE AnimeId = @Id;", new { Id = animeId }, transaction);

            transaction.Commit();

            return animeId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }

    public async Task<bool> CheckIfExists(int id)
    {
        try
        {
            using var connection = _dbConnection.CreateConnection();
            connection.Open();

            var count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Anime WHERE MalId = @MalId;", new { MalId = id });

            return count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    private static async Task SyncStudiosAsync(IDbConnection connection, IDbTransaction transaction, int animeId, List<Studio>? studios)
    {
        if (studios == null) return;

        foreach (var studio in studios)
        {
            if (string.IsNullOrWhiteSpace(studio.Name)) continue;

            var studioId = await connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT StudioId FROM Studio WHERE Name = @Name;", new { studio.Name }, transaction);

            if (studioId == null)
            {
                studioId = await connection.QuerySingleAsync<int>(
                    "INSERT INTO Studio (Name) VALUES (@Name); SELECT last_insert_rowid();", new { studio.Name }, transaction);
            }

            var linkExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM StudioAnime WHERE StudioKey = @StudioKey AND AnimeKey = @AnimeKey;",
                new { StudioKey = studioId, AnimeKey = animeId }, transaction);

            if (linkExists == 0)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO StudioAnime (StudioKey, AnimeKey) VALUES (@StudioKey, @AnimeKey);",
                    new { StudioKey = studioId, AnimeKey = animeId }, transaction);
            }
        }
    }

    private static async Task SyncTagsAsync(IDbConnection connection, IDbTransaction transaction, int animeId, List<MAL_Genre>? genres)
    {
        if (genres == null) return;

        foreach (var genre in genres)
        {
            if (string.IsNullOrWhiteSpace(genre.name)) continue;

            var tagId = await connection.QuerySingleOrDefaultAsync<int?>(
                "SELECT TagId FROM Tag WHERE Name = @Name;", new { Name = genre.name }, transaction);

            if (tagId == null)
            {
                tagId = await connection.QuerySingleAsync<int>(
                    "INSERT INTO Tag (Name) VALUES (@Name); SELECT last_insert_rowid();", new { Name = genre.name }, transaction);
            }

            var linkExists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM TagAnime WHERE TagKey = @TagKey AND AnimeKey = @AnimeKey;",
                new { TagKey = tagId, AnimeKey = animeId }, transaction);

            if (linkExists == 0)
            {
                await connection.ExecuteAsync(
                    "INSERT INTO TagAnime (TagKey, AnimeKey) VALUES (@TagKey, @AnimeKey);",
                    new { TagKey = tagId, AnimeKey = animeId }, transaction);
            }
        }
    }

    private static string? ResolveTitleShort(MAL_Anime malAnime)
    {
        return malAnime.alternative_titles?.synonyms?.MinBy(s => s?.Length ?? int.MaxValue);
    }

    private static int ParseMalCount(string? value)
    {
        return int.TryParse(value, out var result) ? result : 0;
    }
}