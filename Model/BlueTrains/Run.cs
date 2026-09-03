namespace Model.BlueTrains;

public class Run
{
	public int RunId {get;set;}
	public string StaticTripId {get;set;}
	public int LineKey {get;set;}
	public int? ShapeKey {get;set;}
	public string RunNumber {get;set;}
	public string? Headsign {get;set;}
	public bool IsOutbound {get;set;}
	public ServiceClass ServiceClass {get;set;}
	public bool IsSpecial {get;set;}
	public int? RollingStockLevel {get;set;}
	public bool HasCenterBoarding {get;set;}
	public bool HasFlagStops {get;set;}
	public bool BikesAllowed {get;set;}
	public Line Line {get;set;}
	public Shape? Shape {get;set;}
}
