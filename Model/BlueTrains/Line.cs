namespace Model.BlueTrains;

public class Line
{
	public int LineId {get;set;}
	public string StaticRouteId {get;set;}
	public string Name {get;set;}
	public string NameShort {get;set;}
	public string Identifier {get;set;}
	public string ColorHex {get;set;}
	public string TextColorHex {get;set;}
	public string? ScheduleUrl {get;set;}
	public bool Electrified {get;set;}
	public string? PrimaryMover {get;set;}
}
