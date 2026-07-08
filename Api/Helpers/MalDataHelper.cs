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
    
    
    /// <summary>
    /// Maps a calendar month to its MAL season name.
    /// Winter = Dec-Feb, Spring = Mar-May, Summer = Jun-Aug, Fall = Sep-Nov.
    /// </summary>
    public static string ResolveSeasonName(DateTime date)
    {
        return date.Month switch
        {
            1 or 2 or 3 => "winter",
            4 or 5 or 6 => "spring",
            7 or 8 or 9 => "summer",
            _ => "fall"
        };
    }

    public static List<Anime> MalSeasonalAnimeToDbAnime(MAL_Anime_Search malAnimeSearch, int year, string season)
    {
        List<Anime> translatedAnime = new List<Anime>();

        if (malAnimeSearch?.data == null)
            return translatedAnime;

        foreach (var m in malAnimeSearch.data)
        {
            if (m?.node?.start_season == null)
                continue;

            if (m.node.start_season.year == year &&
                string.Equals(m.node.start_season.season, season, StringComparison.OrdinalIgnoreCase))
            {
                translatedAnime.Add(MalNodeToAnime(m.node));
            }
        }

        return translatedAnime;
    }

    public static List<ParsedReccomendedAnime> MalRecommendationsToParsed(List<MAL_Recommendation> recommendations)
    {
        List<ParsedReccomendedAnime> parsed = new List<ParsedReccomendedAnime>();

        if (recommendations == null)
            return parsed;

        foreach (var r in recommendations)
        {
            if (r?.node == null)
                continue;

            parsed.Add(new ParsedReccomendedAnime
            {
                malId = r.node.id,
                title = r.node.title,
                poster = r.node.main_picture?.large ?? r.node.main_picture?.medium
            });
        }

        return parsed;
    }

    public static List<ParsedRelatedAnime> MalRelatedAnimeToParsed(List<MAL_RelatedAnime> relatedAnime)
    {
        List<ParsedRelatedAnime> parsed = new List<ParsedRelatedAnime>();

        if (relatedAnime == null)
            return parsed;

        foreach (var r in relatedAnime)
        {
            if (r?.node == null)
                continue;

            parsed.Add(new ParsedRelatedAnime
            {
                malId = r.node.id,
                title = r.node.title,
                poster = r.node.main_picture?.large ?? r.node.main_picture?.medium,
                relationType = r.relation_type_formatted ?? r.relation_type
            });
        }

        return parsed;
    }

    public static List<string> MalPicturesToStrings(List<MAL_Picture> pictures)
    {
        List<string> parsed = new List<string>();

        if (pictures == null)
            return parsed;

        foreach (var p in pictures)
        {
            var url = p?.large ?? p?.medium;
            if (!string.IsNullOrWhiteSpace(url))
                parsed.Add(url);
        }

        return parsed;
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
            OnAir = DateTimeHelper.ParseFlexible(node.start_date) ?? default,
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
 
        var parsedDate = DateTimeHelper.ParseFlexible(node.start_date);
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


}