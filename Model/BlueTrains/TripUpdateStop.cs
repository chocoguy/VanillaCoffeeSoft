namespace Model.BlueTrains;

public class TripUpdateStop
{
	public int TripUpdateKey {get;set;}
	public int StopSequence {get;set;}
	public string StaticStopId {get;set;}
	public int? StationKey {get;set;}
	public int? PredictedSeconds {get;set;}
	public int? DelaySeconds {get;set;}
	public StopScheduleRelationship ScheduleRelationship {get;set;}
	public PredictionSource Source {get;set;}
}
