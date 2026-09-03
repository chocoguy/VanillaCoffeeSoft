namespace Model.BlueTrains;


//Not tied to SQLite!!! This will never be stored
public class RunPosition
{
    public int locomotiveId { get; set; }
    public string TripId { get; set; }
    public DateTime StartDT { get; set; }
    public string LineIdentifier { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int Bearing { get; set; }
}