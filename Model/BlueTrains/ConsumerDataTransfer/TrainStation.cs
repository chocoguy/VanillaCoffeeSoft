namespace Model.BlueTrains.ConsumerDataTransfer;

public class TrainStation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Identifier { get; set; }
    public string LatLong { get; set; }
    public bool IsAccessible { get; set; }
    public bool IsTerminus  { get; set; }
    public int FareZone { get; set; }
}