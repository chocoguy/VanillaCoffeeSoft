namespace Model.WindyPoints;

public class CreditCard
{
	public int CreditCardId {get;set;}
	public string Name {get;set;}
	public string? Picture {get;set;}
	public string? ColorHex {get;set;}
	public string? ColorRgb {get;set;}
	public string? Advantage {get;set;}
	public bool IsPointCard {get;set;}
	public bool HasAnnualFee {get;set;}
	public bool HasFTF {get;set;}
	public string? Addendum {get;set;}
	public DateTime? Added {get;set;}
	public DateTime? Edited {get;set;}
	public bool IsActive {get;set;}
	public Network Network {get;set;}
	public Issuer Issuer {get;set;}
}
