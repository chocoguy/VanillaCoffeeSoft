namespace Model.BlueTrains;

public enum ServiceClass
{
	/// <summary>Serves effectively every station it passes (coverage >= 0.95).</summary>
	Local = 0,
	/// <summary>Skips some stations (coverage 0.70 - 0.95).</summary>
	Express = 1,
	/// <summary>Skips most stations (coverage 0.45 - 0.70).</summary>
	LimitedExpress = 2,
	/// <summary>Fastest terminus-to-terminus service (coverage &lt; 0.45).</summary>
	SuperExpress = 3
}
