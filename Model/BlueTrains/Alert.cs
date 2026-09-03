namespace Model.BlueTrains;

public class Alert
{
	public int AlertId {get;set;}
	public string FeedEntityId {get;set;}
	public string? StaticRouteId {get;set;}
	public string? AgencyId {get;set;}
	public string HeaderText {get;set;}
	public string? DescriptionText {get;set;}
	public string? Url {get;set;}
	public int Cause {get;set;}
	public int Effect {get;set;}
	public string ContentHash {get;set;}
	public long FirstSeen {get;set;}
	public long LastSeen {get;set;}
	public long? EditedAt {get;set;}
	public long? ClearedAt {get;set;}
}
