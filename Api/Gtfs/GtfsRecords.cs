using CsvHelper.Configuration.Attributes;

namespace Api.Gtfs;

public sealed class GtfsFeed
{
    public List<GtfsCalendar> Calendars { get; init; } = new();
    public List<GtfsCalendarDate> CalendarDates { get; init; } = new();
    public List<GtfsTrip> Trips { get; init; } = new();
    public List<GtfsStopTime> StopTimes { get; init; } = new();
}

public sealed record GtfsCalendar
{
    [Name("service_id")] public string ServiceId { get; init; } = "";
    [Name("monday")] public int Monday { get; init; }
    [Name("tuesday")] public int Tuesday { get; init; }
    [Name("wednesday")] public int Wednesday { get; init; }
    [Name("thursday")] public int Thursday { get; init; }
    [Name("friday")] public int Friday { get; init; }
    [Name("saturday")] public int Saturday { get; init; }
    [Name("sunday")] public int Sunday { get; init; }
    [Name("start_date")] public string StartDate { get; init; } = "";
    [Name("end_date")] public string EndDate { get; init; } = "";
}

public sealed record GtfsCalendarDate
{
    [Name("service_id")] public string ServiceId { get; init; } = "";
    [Name("date")] public string Date { get; init; } = "";
    //1 = service added on this date, 2 = service removed
    [Name("exception_type")] public int ExceptionType { get; init; }
}

public sealed record GtfsTrip
{
    [Name("route_id")] public string RouteId { get; init; } = "";
    [Name("service_id")] public string ServiceId { get; init; } = "";
    [Name("trip_id")] public string TripId { get; init; } = "";
    [Name("trip_headsign")] public string? Headsign { get; init; }
    [Name("shape_id")] public string ShapeId { get; init; } = "";
    //0 = outbound, 1 = inbound
    [Name("direction_id")] public int DirectionId { get; init; }
}

public sealed record GtfsStopTime
{
    [Name("trip_id")] public string TripId { get; init; } = "";
    [Name("departure_time")] public string DepartureTime { get; init; } = "";
    [Name("stop_id")] public string StopId { get; init; } = "";
    [Name("stop_sequence")] public int StopSequence { get; init; }
    [Name("pickup_type")] public int PickupType { get; init; }
    [Name("drop_off_type")] public int DropOffType { get; init; }
    //Metra extension columns beyond standard GTFS:
    [Name("center_boarding")] public int CenterBoarding { get; init; }
    [Name("south_boarding")] public int SouthBoarding { get; init; }
    [Name("bikes_allowed")] public int BikesAllowed { get; init; }
    [Name("notice")] public int Notice { get; init; }
}
