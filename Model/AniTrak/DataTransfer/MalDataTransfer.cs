using System.Text.Json.Serialization;

namespace Model.AniTrak.DataTransfer;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
    public class MAL_AlternativeTitles
    {
        [JsonPropertyName("synonyms")]
        public List<string> synonyms { get; set; }

        [JsonPropertyName("en")]
        public string en { get; set; }

        [JsonPropertyName("ja")]
        public string ja { get; set; }
    }

    public class MAL_Broadcast
    {
        [JsonPropertyName("day_of_the_week")]
        public string day_of_the_week { get; set; }

        [JsonPropertyName("start_time")]
        public string start_time { get; set; }
    }

    public class MAL_Genre
    {
        [JsonPropertyName("id")]
        public int id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }
    }

    public class MAL_MainPicture
    {
        [JsonPropertyName("medium")]
        public string medium { get; set; }

        [JsonPropertyName("large")]
        public string large { get; set; }
    }

    public class MAL_Node
    {
        [JsonPropertyName("id")]
        public int id { get; set; }

        [JsonPropertyName("title")]
        public string title { get; set; }

        [JsonPropertyName("main_picture")]
        public MAL_MainPicture main_picture { get; set; }
        
        [JsonPropertyName("alternative_titles")]
        public MAL_AlternativeTitles alternative_titles { get; set; }
        
        [JsonPropertyName("start_date")]
        public string start_date { get; set; }
        
        [JsonPropertyName("start_season")]
        public MAL_StartSeason start_season { get; set; }
        
        [JsonPropertyName("media_type")]
        public string media_type { get; set; }
        
        [JsonPropertyName("num_episodes")]
        public int num_episodes { get; set; }
    }

    public class MAL_Picture
    {
        [JsonPropertyName("medium")]
        public string medium { get; set; }

        [JsonPropertyName("large")]
        public string large { get; set; }
    }

    public class MAL_Recommendation
    {
        [JsonPropertyName("node")]
        public MAL_Node node { get; set; }

        [JsonPropertyName("num_recommendations")]
        public int num_recommendations { get; set; }
    }

    public class MAL_RelatedAnime
    {
        [JsonPropertyName("node")]
        public MAL_Node node { get; set; }

        [JsonPropertyName("relation_type")]
        public string relation_type { get; set; }

        [JsonPropertyName("relation_type_formatted")]
        public string relation_type_formatted { get; set; }
    }


    public class MAL_Datum
    {
        [JsonPropertyName("node")]
        public MAL_Node node { get; set; }
    }

    public class MAL_Paging
    {
        [JsonPropertyName("next")]
        public string next { get; set; }
    }

    public class MAL_Anime_Search
    {
        [JsonPropertyName("data")]
        public List<MAL_Datum> data { get; set; }

        [JsonPropertyName("paging")]
        public MAL_Paging paging { get; set; }
    }

    public class MAL_Anime
    {
        [JsonPropertyName("id")]
        public int id { get; set; }

        [JsonPropertyName("title")]
        public string title { get; set; }

        [JsonPropertyName("main_picture")]
        public MAL_MainPicture main_picture { get; set; }

        [JsonPropertyName("alternative_titles")]
        public MAL_AlternativeTitles alternative_titles { get; set; }

        [JsonPropertyName("start_date")]
        public string start_date { get; set; }

        [JsonPropertyName("end_date")]
        public string end_date { get; set; }

        [JsonPropertyName("synopsis")]
        public string synopsis { get; set; }

        [JsonPropertyName("mean")]
        public double mean { get; set; }

        [JsonPropertyName("rank")]
        public int rank { get; set; }

        [JsonPropertyName("popularity")]
        public int popularity { get; set; }

        [JsonPropertyName("num_list_users")]
        public int num_list_users { get; set; }

        [JsonPropertyName("num_scoring_users")]
        public int num_scoring_users { get; set; }

        [JsonPropertyName("nsfw")]
        public string nsfw { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime created_at { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime updated_at { get; set; }

        [JsonPropertyName("media_type")]
        public string media_type { get; set; }

        [JsonPropertyName("status")]
        public string status { get; set; }

        [JsonPropertyName("genres")]
        public List<MAL_Genre> genres { get; set; }

        [JsonPropertyName("num_episodes")]
        public int num_episodes { get; set; }

        [JsonPropertyName("start_season")]
        public MAL_StartSeason start_season { get; set; }

        [JsonPropertyName("broadcast")]
        public MAL_Broadcast broadcast { get; set; }

        [JsonPropertyName("source")]
        public string source { get; set; }

        [JsonPropertyName("average_episode_duration")]
        public int average_episode_duration { get; set; }

        [JsonPropertyName("rating")]
        public string rating { get; set; }

        [JsonPropertyName("pictures")]
        public List<MAL_Picture> pictures { get; set; }

        [JsonPropertyName("background")]
        public string background { get; set; }

        [JsonPropertyName("related_anime")]
        public List<MAL_RelatedAnime> related_anime { get; set; }

        [JsonPropertyName("related_manga")]
        public List<object> related_manga { get; set; }

        [JsonPropertyName("recommendations")]
        public List<MAL_Recommendation> recommendations { get; set; }

        [JsonPropertyName("studios")]
        public List<Studio> studios { get; set; }

        [JsonPropertyName("statistics")]
        public MAL_Statistics statistics { get; set; }
    }

    public class MAL_StartSeason
    {
        [JsonPropertyName("year")]
        public int year { get; set; }

        [JsonPropertyName("season")]
        public string season { get; set; }
    }

    public class MAL_Statistics
    {
        [JsonPropertyName("status")]
        public MAL_Status status { get; set; }

        [JsonPropertyName("num_list_users")]
        public int num_list_users { get; set; }
    }

    public class MAL_Status
    {
        [JsonPropertyName("watching")]
        public string watching { get; set; }

        [JsonPropertyName("completed")]
        public string completed { get; set; }

        [JsonPropertyName("on_hold")]
        public string on_hold { get; set; }

        [JsonPropertyName("dropped")]
        public string dropped { get; set; }

        [JsonPropertyName("plan_to_watch")]
        public string plan_to_watch { get; set; }
    }

    public class MAL_Studio
    {
        [JsonPropertyName("id")]
        public int id { get; set; }

        [JsonPropertyName("name")]
        public string name { get; set; }
    }

