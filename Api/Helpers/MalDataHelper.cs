using Model.AniTrak;
using Model.AniTrak.DataTransfer;

namespace Api.Helpers;

public class MalDataHelper
{
    public static List<Anime> MalAnimeSearchToDbAnimeSearch(MAL_Anime_Search malAnimeSearch)
    {
        List<Anime> translatedAnime = new List<Anime>();
        
        foreach (var m in malAnimeSearch.data)
        {
            if (m.node.num_episodes != 0)
            {
                translatedAnime.Add(MalNodeToAnime(m.node));   
            }
        }

        return translatedAnime;

    }
    
    
    public static Anime MalNodeToAnime(MAL_Node node)
    {
        if (node == null)
            return null;
 
        var anime = new Anime
        {
            MalId = node.id,
            Title = node.title,
            TitleShort = ResolveTitleShort(node),
            TitleRomanized = node.alternative_titles?.en,
            TitleKana = node.alternative_titles?.ja,
            EpisodeCount = node.num_episodes,
            Year = node.start_season?.year ?? 2002,
            OnAir = ParseMalDate(node.start_date) ?? default,
            Season = ResolveSeason(node),
            Poster = node.main_picture?.large ?? node.main_picture?.medium,
            MediaType = ResolveMediaType(node.media_type),
            LastSynced = DateTime.UtcNow,
        };
 
        return anime;
    }
 
    private static int ResolveYear(MAL_Node node)
    {
        if (node.start_season?.year > 0)
            return node.start_season.year;
 
        var parsedDate = ParseMalDate(node.start_date);
        return parsedDate?.Year ?? 0;
    }
    
    public static string ResolveTitleShort(MAL_Node node)
    {

        string resolvedTitleShort = "";

        if (node.alternative_titles?.synonyms != null)
        {
            resolvedTitleShort = node.alternative_titles.synonyms.MinBy(s => s?.Length ?? int.MaxValue);
        }
        
        return resolvedTitleShort;
    }


    private static Season ResolveSeason(MAL_Node node)
    {
        
        
        AniTrakDictionary.D_Season.TryGetValue(node.start_season.season, out int resolvedSeasonId);

        Season newSeason = new();
        newSeason.Name = node.start_season.season;
        newSeason.SeasonId = resolvedSeasonId;
        
        return newSeason;

    }

    private static MediaType ResolveMediaType(string malMediaType)
    {
        AniTrakDictionary.D_MediaType.TryGetValue(malMediaType, out int resolvedMediaTypeId);
        
        
        MediaType newMediaType = new();
        newMediaType.Name = malMediaType;
        newMediaType.MediaTypeId = resolvedMediaTypeId;
        
        
        return newMediaType;

    }
 
    /// <summary>
    /// MAL dates can arrive as "yyyy", "yyyy-MM", or "yyyy-MM-dd".
    /// Returns null if the string is empty or unparseable.
    /// </summary>
    private static DateTime? ParseMalDate(string malDate)
    {
        if (string.IsNullOrWhiteSpace(malDate))
            return null;
 
        string[] formats = { "yyyy-MM-dd", "yyyy-MM", "yyyy" };
 
        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(malDate, format,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out var result))
            {
                return result;
            }
        }
 
        // Last resort: let the framework try
        return DateTime.TryParse(malDate, out var fallback) ? fallback : null;
    }
    
    
}