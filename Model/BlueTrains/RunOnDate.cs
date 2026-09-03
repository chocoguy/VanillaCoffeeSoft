namespace Model.BlueTrains;

public class RunOnDate
{
	public string Date {get;set;}
	public int RunId {get;set;}
	public string StaticTripId {get;set;}
	public string RunNumber {get;set;}
	public string? Headsign {get;set;}
	public bool IsOutbound {get;set;}
	public ServiceClass ServiceClass {get;set;}
	public bool IsSpecial {get;set;}
	public string LineIdentifier {get;set;}
	public string StaticRouteId {get;set;}
}
