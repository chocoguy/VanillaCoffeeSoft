namespace Model.BlueTrains.ConsumerDataTransfer;

public class TrainAdvisory
{
    public int Id { get; set; }
    public TrainLine? PostedLine { get; set; } //If null then it's systemwide
    public string Header { get; set; }
    public string Description { get; set; }
    public DateTime Posted { get; set; }
}


//train 323
//Train #2156
//#1296