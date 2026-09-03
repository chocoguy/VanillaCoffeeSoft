using Api.Helpers;
using Model.BlueTrains;

namespace Api.Gtfs;

public static class GtfsImportModelBuilder
{
    private static readonly (string Identifier, HashSet<string> Stations)[] BranchExclusiveStations =
    {
        ("ME-BI", new HashSet<string>
            { "STATEST", "WPULLMAN", "RACINE", "ASHLAND", "BURROAK", "BLUEISLAND", "STEWARTRID" }),
        ("ME-SC", new HashSet<string>
            { "BRYNMAWR", "SOUTHSHORE", "WINDSORPK", "79TH-SC", "83RD-SC", "87TH-SC", "STONYISLND", "93RD-SC" }),
        ("RI-BEV", new HashSet<string>
            { "BRAINERD", "91ST-BEV", "95TH-BEV", "99TH-BEV", "103RD-BEV", "107TH-BEV",
              "111TH-BEV", "115TH-BEV", "119TH-BEV", "123RD-BEV", "PRAIRIEST" }),
        ("UP-NW-MCH", new HashSet<string> { "MCHENRY" })
    };
    
    private static readonly Dictionary<string, string> RunNumberPrefixByRoute = new()
    {
        ["ME"] = "ME", ["BNSF"] = "BN", ["UP-NW"] = "UNW", ["RI"] = "RI",
        ["UP-N"] = "UN", ["UP-W"] = "UW", ["MD-W"] = "MW", ["MD-N"] = "MN",
        ["SWS"] = "SW", ["NCS"] = "NC", ["HC"] = "HC"
    };

    //Service class thresholds
    private const double LocalCoverage = 0.95;
    private const double ExpressCoverage = 0.70;
    private const double LimitedExpressCoverage = 0.45;

    //A run touching none of these is considered to be special (Along with Extra "Special" runs, RAV001, Air & Water show trains, Lolla trains, etc...)
    private static readonly string[] DowntownTerminals = { "CUS", "OTC", "MILLENNIUM", "LSS" };

    public static GtfsImportModel Build(GtfsFeed feed, StaticLookups lookups, DateOnly today,
        Action<string> logWarning)
    {
        var datesByService = ExpandCalendar(feed, today);
        var dayTypeByService = DayTypesByService(feed, datesByService);
        var stopTimesByTrip = feed.StopTimes.GroupBy(st => st.TripId).ToDictionary(g => g.Key, g => g.OrderBy(st => st.StopSequence).ToList());

        var seenTripIds = new HashSet<string>();
        var runs = new List<RunImport>();

        foreach (var trip in feed.Trips)
        {
            if (!seenTripIds.Add(trip.TripId))
            {
                throw new GtfsImportException($"Duplicate trip_id '{trip.TripId}' in trips.txt");
            }

            if (!stopTimesByTrip.TryGetValue(trip.TripId, out var stopTimes))
            {
                logWarning($"Trip '{trip.TripId}' has no stop_times rows; skipping");
                continue;
            }

            var stops = new List<StopImport>(stopTimes.Count);

            foreach (var st in stopTimes)
            {
                if (!lookups.StationIdByIdentifier.TryGetValue(st.StopId, out var stationKey))
                {
                    throw new GtfsImportException($"Unknown stop_id '{st.StopId}' on trip '{trip.TripId}' — Station table needs curation");
                }
                
                var noPickup = st.PickupType == 1;

                stops.Add(new StopImport
                {
                    StationKey = stationKey,
                    Sequence = st.StopSequence,
                    DepartureSeconds = GtfsDateTimeHelper.ParseServiceTimeToSeconds(st.DepartureTime),
                    HasNotice = st.Notice == 1,
                    CenterBoarding = st.CenterBoarding == 1,
                    SouthBoarding = st.SouthBoarding == 1,
                    BikesAllowed = st.BikesAllowed == 1,
                    NoPickup = noPickup,
                    FlagStop = !noPickup && (st.PickupType == 3 || st.DropOffType == 3)
                });
            }

            int? shapeKey = null;
            if (lookups.ShapeIdByStaticId.TryGetValue(trip.ShapeId, out var sk))
            {
                shapeKey = sk;
            }
            else
            {
                logWarning($"Unknown shape_id '{trip.ShapeId}' on trip '{trip.TripId}'; ShapeKey left NULL");
            }

            if (!datesByService.TryGetValue(trip.ServiceId, out var dates))
            {
                logWarning($"Trip '{trip.TripId}' references unknown service_id '{trip.ServiceId}'; no dates");
                dates = new HashSet<DateOnly>();
            }

            runs.Add(new RunImport
            {
                StaticTripId = trip.TripId,
                LineKey = ResolveLineKey(trip, stopTimes, lookups, logWarning),
                ShapeKey = shapeKey,
                RunNumber = ExtractRunNumber(trip, logWarning),
                Headsign = string.IsNullOrWhiteSpace(trip.Headsign) ? null : trip.Headsign,
                IsOutbound = trip.DirectionId == 0,
                DayType = dayTypeByService.TryGetValue(trip.ServiceId, out var dt) ? dt : ServiceDayType.Weekday,
                HasCenterBoarding = stops.Any(s => s.CenterBoarding),
                HasFlagStops = stops.Any(s => s.FlagStop),
                BikesAllowed = stops.All(s => s.BikesAllowed),
                Dates = dates.Order().Select(GtfsDateTimeHelper.ToIsoDate).ToList(),
                Stops = stops
            });
        }

        ClassifyRuns(runs, lookups, logWarning);

        return new GtfsImportModel { Runs = runs };
    }
    
    private static Dictionary<string, HashSet<DateOnly>> ExpandCalendar(GtfsFeed feed, DateOnly today)
    {
        var datesByService = new Dictionary<string, HashSet<DateOnly>>();

        HashSet<DateOnly> SetFor(string serviceId)
        {
            if (!datesByService.TryGetValue(serviceId, out var set))
            {
                datesByService[serviceId] = set = new HashSet<DateOnly>();
            }

            return set;
        }

        foreach (var cal in feed.Calendars)
        {
            var set = SetFor(cal.ServiceId);
            var start = GtfsDateTimeHelper.ParseServiceDate(cal.StartDate);
            var end = GtfsDateTimeHelper.ParseServiceDate(cal.EndDate);

            if (start < today)
            {
                start = today;
            }
                

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                if (RunsOnWeekday(cal, date.DayOfWeek))
                {
                    set.Add(date);
                }
            }
        }

        foreach (var exception in feed.CalendarDates.Where(cd => cd.ExceptionType == 1))
        {
            var date = GtfsDateTimeHelper.ParseServiceDate(exception.Date);
            if (date >= today)
            {
                SetFor(exception.ServiceId).Add(date);
            }
        }

        foreach (var exception in feed.CalendarDates.Where(cd => cd.ExceptionType == 2))
        {
            if (datesByService.TryGetValue(exception.ServiceId, out var set))
            {
                set.Remove(GtfsDateTimeHelper.ParseServiceDate(exception.Date));
            }
        }

        return datesByService;
    }

    private static bool RunsOnWeekday(GtfsCalendar cal, DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.Monday:
                return cal.Monday == 1;
            case DayOfWeek.Tuesday:
                return cal.Tuesday == 1;
            case DayOfWeek.Wednesday:
                return cal.Wednesday == 1;
            case DayOfWeek.Thursday:
                return cal.Thursday == 1;
            case DayOfWeek.Friday:
                return cal.Friday == 1;
            case DayOfWeek.Saturday:
                return cal.Saturday == 1;
            case DayOfWeek.Sunday:
                return cal.Sunday == 1;
            default:
                throw new ArgumentOutOfRangeException(nameof(day), day, null);
        }
    }

    private static int ResolveLineKey(GtfsTrip trip, List<GtfsStopTime> stopTimes,
        StaticLookups lookups, Action<string> logWarning)
    {
        var stopIds = stopTimes.Select(st => st.StopId).ToHashSet();

        string? branch = null;
        foreach (var (identifier, stations) in BranchExclusiveStations)
        {
            if (!stopIds.Overlaps(stations))
                continue;

            if (branch == null)
            {
                branch = identifier;
            }
            else
            {
                logWarning($"Trip '{trip.TripId}' matches branches '{branch}' and '{identifier}'; keeping '{branch}'");
            }
        }

        var lineIdentifier = branch ?? trip.RouteId;

        if (!lookups.LineIdByIdentifier.TryGetValue(lineIdentifier, out var lineKey))
        {
            throw new GtfsImportException($"Unknown line identifier '{lineIdentifier}' (route_id '{trip.RouteId}') — Line table needs curation");
        }

        return lineKey;
    }

    private static string ExtractRunNumber(GtfsTrip trip, Action<string> logWarning)
    {
        var segments = trip.TripId.Split('_');

        if (segments.Length < 2)
        {
            logWarning($"Trip id '{trip.TripId}' has no run-number segment; using the whole id");
            return trip.TripId;
        }

        var token = segments[1];

        if (RunNumberPrefixByRoute.TryGetValue(trip.RouteId, out var prefix)
            && token.Length > prefix.Length
            && token.StartsWith(prefix, StringComparison.Ordinal)
            && token[prefix.Length..].All(char.IsAsciiDigit))
        {
            return token[prefix.Length..];
        }

        return token;
    }


    private static Dictionary<string, ServiceDayType> DayTypesByService(GtfsFeed feed, Dictionary<string, HashSet<DateOnly>> datesByService)
    {
        var dayTypes = new Dictionary<string, ServiceDayType>();

        foreach (var cal in feed.Calendars)
        {
            var weekday = cal.Monday == 1 || cal.Tuesday == 1 || cal.Wednesday == 1
                          || cal.Thursday == 1 || cal.Friday == 1;
            dayTypes[cal.ServiceId] = weekday ? ServiceDayType.Weekday : ServiceDayType.Weekend;
        }

        foreach (var (serviceId, dates) in datesByService)
        {
            if (dayTypes.ContainsKey(serviceId))
                continue;

            dayTypes[serviceId] = dates.Any(d => d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                ? ServiceDayType.Weekday
                : ServiceDayType.Weekend;
        }

        return dayTypes;
    }


    private static void ClassifyRuns(List<RunImport> runs, StaticLookups lookups,
        Action<string> logWarning)
    {
        var terminals = DowntownTerminals.Where(lookups.StationIdByIdentifier.ContainsKey).Select(id => lookups.StationIdByIdentifier[id]).ToHashSet();

        foreach (var group in runs.GroupBy(r => (r.LineKey, r.IsOutbound, r.DayType)))
        {
            var members = group.ToList();
            var corridor = BuildCorridor(members);

            var position = new Dictionary<int, int>(corridor.Count);
            for (var i = 0; i < corridor.Count; i++)
            {
                position.TryAdd(corridor[i], i);
            }

            foreach (var run in members)
            {
                var first = run.Stops[0].StationKey;
                var last = run.Stops[^1].StationKey;

                //Ravinia extra is Special AND an Express.
                run.IsSpecial = !run.RunNumber.All(char.IsAsciiDigit)
                                || (!terminals.Contains(first) && !terminals.Contains(last));

                if (!position.TryGetValue(first, out var lo) || !position.TryGetValue(last, out var hi))
                {
                    logWarning($"Run '{run.StaticTripId}' has endpoints off its line corridor; classified Local");
                    run.ServiceClass = ServiceClass.Local;
                    continue;
                }

                if (lo > hi)
                    (lo, hi) = (hi, lo);

                var passed = hi - lo + 1;
                var coverage = (double)run.Stops.Count / passed;

                run.ServiceClass = coverage >= LocalCoverage ? ServiceClass.Local
                    : coverage >= ExpressCoverage ? ServiceClass.Express
                    : coverage >= LimitedExpressCoverage ? ServiceClass.LimitedExpress
                    : ServiceClass.SuperExpress;
            }
        }
    }
    
    private static List<int> BuildCorridor(List<RunImport> group)
    {
        var order = group.MaxBy(r => r.Stops.Count)!.Stops.Select(s => s.StationKey).ToList();
        var missing = group.SelectMany(r => r.Stops.Select(s => s.StationKey)).ToHashSet();
        missing.ExceptWith(order);

        while (missing.Count > 0)
        {
            var position = new Dictionary<int, int>(order.Count);
            for (var i = 0; i < order.Count; i++)
            {
                position.TryAdd(order[i], i);
            }

            var inserted = false;

            foreach (var run in group)
            {
                var stations = run.Stops.Select(s => s.StationKey).ToList();

                for (var i = 0; i < stations.Count && !inserted; i++)
                {
                    if (!missing.Contains(stations[i]))
                        continue;

                    int? before = null, after = null;
                    for (var j = i - 1; j >= 0 && before == null; j--)
                        if (position.TryGetValue(stations[j], out var p)) before = p;
                    for (var j = i + 1; j < stations.Count && after == null; j++)
                        if (position.TryGetValue(stations[j], out var p)) after = p;

                    if (before == null && after == null)
                        continue;

                    order.Insert(before != null ? before.Value + 1 : after!.Value, stations[i]);
                    missing.Remove(stations[i]);
                    inserted = true;
                }

                if (inserted)
                    break;
            }
            
            if (!inserted)
                break;
        }

        return order;
    }
}
