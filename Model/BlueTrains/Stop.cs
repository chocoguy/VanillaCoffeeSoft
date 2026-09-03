namespace Model.BlueTrains;

public class Stop
{
	public int StopId {get;set;}
	public int RunKey {get;set;}
	public int StationKey {get;set;}
	public int Sequence {get;set;}
	public int DepartureSeconds {get;set;}
	public bool HasNotice {get;set;}
	public bool CenterBoarding {get;set;}
	public bool SouthBoarding {get;set;}
	public bool BikesAllowed {get;set;}
	public bool FlagStop {get;set;}
	public bool NoPickup {get;set;}
	public Station Station {get;set;}
}
