namespace Model.BlueTrains;

public class LivePrediction
{
	public string StaticTripId {get;set;}
	public string ServiceDate {get;set;}
	public int? RunKey {get;set;}
	public string? StaticRouteId {get;set;}
	public long FeedTimestamp {get;set;}
	public int StopSequence {get;set;}
	public string StaticStopId {get;set;}
	public int? StationKey {get;set;}
	public int? PredictedSeconds {get;set;}
	public int? DelaySeconds {get;set;}
	public PredictionSource Source {get;set;}
}
