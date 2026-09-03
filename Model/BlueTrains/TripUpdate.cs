namespace Model.BlueTrains;

public class TripUpdate
{
	public int TripUpdateId {get;set;}
	public string StaticTripId {get;set;}
	public string ServiceDate {get;set;}
	public int? RunKey {get;set;}
	public string? StaticRouteId {get;set;}
	public string? VehicleId {get;set;}
	public TripRelationship TripRelationship {get;set;}
	public long FeedTimestamp {get;set;}
	public long UpdatedAt {get;set;}
	public List<TripUpdateStop> Stops {get;set;} = new();
}
