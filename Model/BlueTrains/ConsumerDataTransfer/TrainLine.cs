namespace Model.BlueTrains.ConsumerDataTransfer;

public class TrainLine
{
    public int Id { get; set; }
    public string NameShort { get; set; }
    public string NameLong { get; set; }
    public string Color { get; set; }
    public string TextColor { get; set; }
    public Uri? TimetablePdf { get; set; } //https://schedules.metrarail.com/pdf/MD-W.pdf
    public bool IsElectrified { get; set; }
    public string PrimaryMover { get; set; }
    
}