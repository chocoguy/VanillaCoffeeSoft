namespace Model.WindyPoints;

public class PointRate
{
	public int PointRateId {get;set;}
	public int CreditCardKey {get;set;}
	public double PointMultiplier {get;set;}
	public double CentsPerPoint {get;set;}
	public double EffectiveCashback {get;set;}
	public string? Icon {get;set;}
	public string? IconSF {get;set;}
	public DateTime Added {get;set;}
	public DateTime Edited {get;set;}
	public bool IsActive {get;set;}
	public SpendCategory SpendCategory {get;set;}
}
