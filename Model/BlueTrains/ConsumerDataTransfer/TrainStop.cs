namespace Model.BlueTrains.ConsumerDataTransfer;

public class TrainStop
{
    public int Id { get; set; }
    public TrainStation Station { get; set; }
    public int StopNumber { get; set; }
    public DateTime DepartureTime { get; set; }
    public bool HasNotice { get; set; }
    public bool CenterBoarding { get; set; }
    public bool SouthBoarding { get; set; }
    public bool BikesAllowed { get; set; }
    public bool IsFlagStop { get; set; }
    public bool NoPickup { get; set; }
}