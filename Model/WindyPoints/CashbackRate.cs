namespace Model.WindyPoints;

public class CashbackRate
{
	public int CashbackRateId {get;set;}
	public int CreditCardKey {get;set;}
	public double CashbackMultiplier {get;set;}
	public string? Icon {get;set;}
	public string? IconSF {get;set;}
	public DateTime Added {get;set;}
	public DateTime Edited {get;set;}
	public bool IsActive {get;set;}
	public SpendCategory SpendCategory {get;set;}
}
