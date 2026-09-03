namespace Model.BlueTrains;

public class Station
{
	public int StationId {get;set;}
	public string Name {get;set;}
	public string Identifier {get;set;}
	public double Lat {get;set;}
	public double Lon {get;set;}
	public int? FareZone {get;set;}
	public bool Accessible {get;set;}
	public bool IsTerminus {get;set;}
}
