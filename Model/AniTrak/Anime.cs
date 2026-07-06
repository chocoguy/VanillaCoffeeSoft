namespace Model.AniTrak;

public class Anime
{
	public int AnimeId {get;set;}
	public int MalId {get;set;}
	public string Title {get;set;}
	public string TitleShort { get; set; }
	public string TitleRomanized {get;set;}
	public string TitleKana {get;set;}
	public int Year {get;set;}
	public int EpisodeCount {get;set;}
	public string AirTime {get;set;}
	public DateTime OnAir {get;set;}
	public DateTime OffAir {get;set;}
	public string Synopsis {get;set;}
	public string Reception {get;set;}
	public double MalScore {get;set;}
	public int MalRank {get;set;}
	public int MalWatching {get;set;}
	public int MalCompleted {get;set;}
	public int MalOnHold {get;set;}
	public int MalDropped {get;set;}
	public int MalPlanToWatch {get;set;}
	public string Poster {get;set;}
	public int EpisodeLength {get;set;}
	public DateTime LastSynced {get;set;}
	public DateTime LastModified {get;set;}
	public Season Season {get;set;}
	public AirDay AirDay {get;set;}
	public MediaType MediaType {get;set;}
	public OriginalSource OriginalSource {get;set;}
	public Nsfw Nsfw {get;set;}
	public List<string> Studios {get;set;}
	public List<string> Tags {get;set;}
	
	
}