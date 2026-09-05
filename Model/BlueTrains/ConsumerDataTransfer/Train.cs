namespace Model.BlueTrains.ConsumerDataTransfer;

public class Train
{
    public int Id { get; set; }
    public string TrainNumber { get; set; }
    public int ServiceClass { get; set; }
    public bool IsOutbound { get; set; }
    public string HeadSign { get; set; }
    public int RollingStockLevel { get; set; }
    public bool HasCenterBoarding  { get; set; }
    public bool HasFlagStops { get; set; }
    public bool BikesAllowed { get; set; }
    public bool IsSpecialService { get; set; }
    public TrainLine Line { get; set; }
    public List<TrainStop> Stops { get; set; }
}