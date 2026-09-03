using Model.BlueTrains;

namespace Api.Gtfs;

public sealed class GtfsImportException : Exception
{
    public GtfsImportException(string message) : base(message) { }
}

public sealed record StaticLookups(
    Dictionary<string, int> LineIdByIdentifier,
    Dictionary<string, int> StationIdByIdentifier,
    Dictionary<string, int> ShapeIdByStaticId);

public sealed class GtfsImportModel
{
    public List<RunImport> Runs { get; init; } = new();

    public int RunDateCount => Runs.Sum(r => r.Dates.Count);
    public int StopCount => Runs.Sum(r => r.Stops.Count);
}

public sealed class RunImport
{
    public required string StaticTripId { get; init; }
    public required int LineKey { get; init; }
    public int? ShapeKey { get; init; }
    public required string RunNumber { get; init; }
    public string? Headsign { get; init; }
    public bool IsOutbound { get; init; }
    public required ServiceDayType DayType { get; init; }
    public ServiceClass ServiceClass { get; set; }
    public bool IsSpecial { get; set; }
    public bool HasCenterBoarding { get; init; }
    public bool HasFlagStops { get; init; }
    public bool BikesAllowed { get; init; }
    //yyyy-MM-dd
    public required List<string> Dates { get; init; }
    public required List<StopImport> Stops { get; init; }
}

public sealed class StopImport
{
    public required int StationKey { get; init; }
    public required int Sequence { get; init; }
    public required int DepartureSeconds { get; init; }
    public bool HasNotice { get; init; }
    public bool CenterBoarding { get; init; }
    public bool SouthBoarding { get; init; }
    public bool BikesAllowed { get; init; }
    public bool FlagStop { get; init; }
    public bool NoPickup { get; init; }
}

public enum ServiceDayType
{
    Weekday,
    Weekend
}
