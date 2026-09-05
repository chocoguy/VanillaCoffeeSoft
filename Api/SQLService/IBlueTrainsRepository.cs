using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using Api.Helpers;
using Dapper;
using Model.BlueTrains.ConsumerDataTransfer;

namespace Api.SQLService;

public interface IBlueTrainsRepository
{
    Task<Train?> GetTrainAsync(int runId, DateOnly? serviceDate = null);

    //Trains running that day in departure order
    Task<IReadOnlyList<Train>?> GetTrainsByLineAndDateAsync(int lineId, DateOnly serviceDate);

    Task<TrainStation?> GetStationAsync(int stationId);

    
    // Stations served by the line ordered outbound from the downtown terminal.
    Task<IReadOnlyList<TrainStation>?> GetStationsByLineAsync(int lineId);

    Task<TrainLine?> GetLineAsync(int lineId);

    Task<IReadOnlyList<TrainLine>> GetAllLinesAsync();

    Task<TrainAdvisory?> GetAdvisoryAsync(int advisoryId);
    
    Task<IReadOnlyList<TrainAdvisory>?> GetAdvisoriesByLineAsync(int lineId);
    
    Task<IReadOnlyList<TrainAdvisory>?> GetAdvisoriesByTrainNumberAsync(int trainNumber);

    Task<IReadOnlyList<TrainAdvisory>> GetSystemwideAdvisoriesAsync();
}

public class BlueTrainsRepository : IBlueTrainsRepository
{
    private const int OffPeakRollingStockLevel = 1;
    private const int PeakRollingStockLevel = 2;

    //Weekday peak windows as seconds into the Chicago service day.
    private const int MorningPeakStartSeconds = 7 * 3600;   // 07:00
    private const int MorningPeakEndSeconds = 9 * 3600;     // 09:00
    private const int EveningPeakStartSeconds = 16 * 3600;  // 16:00
    private const int EveningPeakEndSeconds = 18 * 3600;    // 18:00

    private const string LineColumns =
        """
        l.LineId, l.StaticRouteId, l.Identifier, l.Name AS LineName, l.NameShort AS LineNameShort,
        l.ColorHex, l.TextColorHex, l.ScheduleUrl, l.Electrified, l.PrimaryMover
        """;

    private const string StationColumns =
        """
        st.StationId, st.Name AS StationName, st.Identifier, st.Lat, st.Lon,
        st.FareZone, st.Accessible, st.IsTerminus
        """;

    private const string RunColumns =
        """
        r.RunId, r.RunNumber, r.Headsign, r.IsOutbound, r.ServiceClass, r.IsSpecial,
        r.HasCenterBoarding, r.HasFlagStops, r.BikesAllowed
        """;

    //Sequence is non-contiguous in the GTFS feed, so hand consumers a dense 1..N position
    //and keep Sequence purely as the sort key.
    private const string StopColumns =
        """
        s.RunKey, s.StopId,
        ROW_NUMBER() OVER (PARTITION BY s.RunKey ORDER BY s.Sequence) AS StopNumber,
        s.DepartureSeconds, s.HasNotice, s.CenterBoarding, s.SouthBoarding,
        s.BikesAllowed, s.FlagStop, s.NoPickup
        """;

    private readonly IBlueTrainsStaticDbConnectionFactory _dbConnection;
    private readonly IBlueTrainsRealtimeDbConnectionFactory _realtimeDbConnection;
    private readonly ILogger<BlueTrainsRepository> _logger;

    public BlueTrainsRepository(IBlueTrainsStaticDbConnectionFactory dbConnection,
        IBlueTrainsRealtimeDbConnectionFactory realtimeDbConnection,
        ILogger<BlueTrainsRepository> logger)
    {
        _dbConnection = dbConnection;
        _realtimeDbConnection = realtimeDbConnection;
        _logger = logger;
    }

    public async Task<Train?> GetTrainAsync(int runId, DateOnly? serviceDate = null)
    {
        using var connection = OpenStatic();

        var run = await connection.QuerySingleOrDefaultAsync<RunRow>(
            $"SELECT {RunColumns}, {LineColumns} FROM Run r JOIN Line l ON l.LineId = r.LineKey WHERE r.RunId = @RunId;",
            new { RunId = runId });

        if (run is null)
            return null;

        var date = serviceDate ?? await ResolveServiceDateAsync(connection, runId);

        var stops = (await connection.QueryAsync<StopRow>(
            $"""
             SELECT {StopColumns}, {StationColumns}
             FROM   Stop s JOIN Station st ON st.StationId = s.StationKey
             WHERE  s.RunKey = @RunId
             ORDER  BY s.Sequence;
             """,
            new { RunId = runId })).ToList();

        return ToTrain(run, stops, date);
    }

    public async Task<IReadOnlyList<Train>?> GetTrainsByLineAndDateAsync(int lineId, DateOnly serviceDate)
    {
        using var connection = OpenStatic();

        if (!await LineExistsAsync(connection, lineId))
            return null;

        var date = GtfsDateTimeHelper.ToIsoDate(serviceDate);
        var parameters = new { LineId = lineId, Date = date };

        var runs = (await connection.QueryAsync<RunRow>(
            $"""
             SELECT {RunColumns}, {LineColumns}
             FROM   RunDate rd
                    JOIN Run  r ON r.RunId  = rd.RunKey
                    JOIN Line l ON l.LineId = r.LineKey
             WHERE  rd.Date = @Date AND r.LineKey = @LineId
             ORDER  BY (SELECT MIN(s.DepartureSeconds) FROM Stop s WHERE s.RunKey = r.RunId), r.RunNumber;
             """,
            parameters)).ToList();

        if (runs.Count == 0)
            return [];

        // One pass for every stop on the day rather than a query per run.
        var stopsByRun = (await connection.QueryAsync<StopRow>(
                $"""
                 SELECT {StopColumns}, {StationColumns}
                 FROM   Stop s JOIN Station st ON st.StationId = s.StationKey
                 WHERE  s.RunKey IN (SELECT rd.RunKey
                                     FROM   RunDate rd JOIN Run r ON r.RunId = rd.RunKey
                                     WHERE  rd.Date = @Date AND r.LineKey = @LineId)
                 ORDER  BY s.RunKey, s.Sequence;
                 """,
                parameters))
            .GroupBy(s => s.RunKey)
            .ToDictionary(g => g.Key, IReadOnlyList<StopRow> (g) => g.ToList());

        return runs
            .Select(r => ToTrain(r, stopsByRun.GetValueOrDefault(r.RunId, []), serviceDate))
            .ToList();
    }

    public async Task<TrainStation?> GetStationAsync(int stationId)
    {
        using var connection = OpenStatic();

        var station = await connection.QuerySingleOrDefaultAsync<StationRow>(
            $"SELECT {StationColumns} FROM Station st WHERE st.StationId = @StationId;",
            new { StationId = stationId });

        return station is null ? null : ToTrainStation(station);
    }

    public async Task<IReadOnlyList<TrainStation>?> GetStationsByLineAsync(int lineId)
    {
        using var connection = OpenStatic();

        if (!await LineExistsAsync(connection, lineId))
            return null;

        // Outbound sequences climb away from downtown; inbound ones climb toward it, so a
        // line with no outbound runs is ordered by its negated inbound average instead.
        var stations = await connection.QueryAsync<StationRow>(
            $"""
             SELECT {StationColumns}
             FROM   Station st
                    JOIN Stop s ON s.StationKey = st.StationId
                    JOIN Run  r ON r.RunId      = s.RunKey
             WHERE  r.LineKey = @LineId
             GROUP  BY st.StationId
             ORDER  BY COALESCE(AVG(CASE WHEN r.IsOutbound = 1 THEN s.Sequence END),
                               -AVG(CASE WHEN r.IsOutbound = 0 THEN s.Sequence END));
             """,
            new { LineId = lineId });

        return stations.Select(ToTrainStation).ToList();
    }

    public async Task<TrainLine?> GetLineAsync(int lineId)
    {
        using var connection = OpenStatic();

        var line = await connection.QuerySingleOrDefaultAsync<LineRow>(
            $"SELECT {LineColumns} FROM Line l WHERE l.LineId = @LineId;", new { LineId = lineId });

        return line is null ? null : ToTrainLine(line);
    }

    public async Task<IReadOnlyList<TrainLine>> GetAllLinesAsync()
    {
        using var connection = OpenStatic();

        var lines = await connection.QueryAsync<LineRow>(
            $"SELECT {LineColumns} FROM Line l ORDER BY l.Identifier;");

        return lines.Select(ToTrainLine).ToList();
    }

    public async Task<TrainAdvisory?> GetAdvisoryAsync(int advisoryId)
    {
        using var realtime = OpenRealtime();

        //Cleared advisories still resolve
        var alert = await realtime.QuerySingleOrDefaultAsync<AlertRow>(
            """
            SELECT AlertId, StaticRouteId, HeaderText, DescriptionText, FirstSeen
            FROM   Alert
            WHERE  AlertId = @AlertId;
            """,
            new { AlertId = advisoryId });

        if (alert is null)
            return null;

        if (alert.StaticRouteId is null)
            return ToTrainAdvisory(alert, ReadOnlyDictionary<string, TrainLine>.Empty);

        using var connection = OpenStatic();

        return ToTrainAdvisory(alert, await GetLinesByRouteAsync(connection));
    }

    public async Task<IReadOnlyList<TrainAdvisory>?> GetAdvisoriesByLineAsync(int lineId)
    {
        using var connection = OpenStatic();

        var route = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT StaticRouteId FROM Line WHERE LineId = @LineId;", new { LineId = lineId });

        if (route is null)
            return null;

        using var realtime = OpenRealtime();
        
        var alerts = await realtime.QueryAsync<AlertRow>(
            """
            SELECT AlertId, StaticRouteId, HeaderText, DescriptionText, FirstSeen
            FROM   Alert
            WHERE  StaticRouteId = @Route AND ClearedAt IS NULL
            ORDER  BY FirstSeen DESC;
            """,
            new { Route = route });

        var linesByRoute = await GetLinesByRouteAsync(connection);

        return alerts.Select(a => ToTrainAdvisory(a, linesByRoute)).ToList();
    }

    public async Task<IReadOnlyList<TrainAdvisory>?> GetAdvisoriesByTrainNumberAsync(int trainNumber)
    {
        using var connection = OpenStatic();
        
        var number = trainNumber.ToString(CultureInfo.InvariantCulture);

        var runExists = await connection.ExecuteScalarAsync<int?>(
            "SELECT 1 FROM Run WHERE RunNumber = @Number LIMIT 1;", new { Number = number }) is not null;

        if (!runExists)
            return null;

        using var realtime = OpenRealtime();
        
        var alerts = await realtime.QueryAsync<AlertRow>(
            """
            SELECT AlertId, StaticRouteId, HeaderText, DescriptionText, FirstSeen
            FROM   Alert
            WHERE  ClearedAt IS NULL
            ORDER  BY FirstSeen DESC;
            """);

        var matches = alerts
            .Where(a => AdvisoryTrainNumberParser.Mentions(trainNumber, a.HeaderText, a.DescriptionText))
            .ToList();

        if (matches.Count == 0)
            return [];

        var linesByRoute = await GetLinesByRouteAsync(connection);

        return matches.Select(a => ToTrainAdvisory(a, linesByRoute)).ToList();
    }

    public async Task<IReadOnlyList<TrainAdvisory>> GetSystemwideAdvisoriesAsync()
    {
        using var realtime = OpenRealtime();

        var alerts = await realtime.QueryAsync<AlertRow>(
            """
            SELECT AlertId, StaticRouteId, HeaderText, DescriptionText, FirstSeen
            FROM   Alert
            WHERE  StaticRouteId IS NULL AND ClearedAt IS NULL
            ORDER  BY FirstSeen DESC;
            """);

        return alerts.Select(a => ToTrainAdvisory(a, ReadOnlyDictionary<string, TrainLine>.Empty)).ToList();
    }
    
    private static async Task<Dictionary<string, TrainLine>> GetLinesByRouteAsync(IDbConnection connection)
    {
        var lines = await connection.QueryAsync<LineRow>($"SELECT {LineColumns} FROM Line l;");

        return lines
            .GroupBy(l => l.StaticRouteId)
            .ToDictionary(
                g => g.Key,
                g => ToTrainLine(g.OrderByDescending(l => l.Identifier == g.Key)
                    .ThenBy(l => l.LineId)
                    .First()));
    }
    
    private static async Task<bool> LineExistsAsync(IDbConnection connection, int lineId) =>
        await connection.ExecuteScalarAsync<int?>(
            "SELECT 1 FROM Line WHERE LineId = @LineId;", new { LineId = lineId }) is not null;

    private static async Task<DateOnly> ResolveServiceDateAsync(IDbConnection connection, int runId)
    {
        var today = GtfsDateTimeHelper.ToIsoDate(GtfsDateTimeHelper.TodayInChicago(DateTime.UtcNow));

        // Dates are stored as 'YYYY-MM-DD', so a string compare orders them correctly.
        var date = await connection.QuerySingleOrDefaultAsync<string>(
            """
            SELECT COALESCE(
                (SELECT MIN(Date) FROM RunDate WHERE RunKey = @RunId AND Date >= @Today),
                (SELECT MAX(Date) FROM RunDate WHERE RunKey = @RunId));
            """,
            new { RunId = runId, Today = today });

        return date is null
            ? GtfsDateTimeHelper.TodayInChicago(DateTime.UtcNow)
            : DateOnly.ParseExact(date, GtfsDateTimeHelper.IsoDateFormat, CultureInfo.InvariantCulture);
    }

    private static Train ToTrain(RunRow run, IReadOnlyList<StopRow> orderedStops, DateOnly serviceDate) => new()
    {
        Id = run.RunId,
        TrainNumber = run.RunNumber,
        ServiceClass = run.ServiceClass,
        IsOutbound = run.IsOutbound,
        HeadSign = run.Headsign ?? string.Empty,
        RollingStockLevel = ResolveRollingStockLevel(run, orderedStops, serviceDate),
        HasCenterBoarding = run.HasCenterBoarding,
        HasFlagStops = run.HasFlagStops,
        BikesAllowed = run.BikesAllowed,
        IsSpecialService = run.IsSpecial,
        Line = ToTrainLine(run),
        Stops = orderedStops.Select(s => ToTrainStop(s, serviceDate)).ToList()
    };


    private static int ResolveRollingStockLevel(RunRow run, IReadOnlyList<StopRow> orderedStops, DateOnly serviceDate)
    {
        if (orderedStops.Count == 0 || serviceDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            return OffPeakRollingStockLevel;
        
        var downtownSeconds = run.IsOutbound
            ? orderedStops[0].DepartureSeconds
            : orderedStops[^1].DepartureSeconds;
        
        var isPeak =
            (downtownSeconds >= MorningPeakStartSeconds && downtownSeconds <= MorningPeakEndSeconds)
            || (downtownSeconds >= EveningPeakStartSeconds && downtownSeconds <= EveningPeakEndSeconds);

        return isPeak ? PeakRollingStockLevel : OffPeakRollingStockLevel;
    }

    private static TrainStop ToTrainStop(StopRow stop, DateOnly serviceDate) => new()
    {
        Id = stop.StopId,
        Station = ToTrainStation(stop),
        StopNumber = stop.StopNumber,
        DepartureTime = GtfsDateTimeHelper.ComposeUtcInstant(serviceDate, stop.DepartureSeconds),
        HasNotice = stop.HasNotice,
        CenterBoarding = stop.CenterBoarding,
        SouthBoarding = stop.SouthBoarding,
        BikesAllowed = stop.BikesAllowed,
        IsFlagStop = stop.FlagStop,
        NoPickup = stop.NoPickup
    };

    private static TrainStation ToTrainStation(IStationColumns station) => new()
    {
        Id = station.StationId,
        Name = station.StationName,
        Identifier = station.Identifier,
        LatLong = string.Create(CultureInfo.InvariantCulture, $"{station.Lat},{station.Lon}"),
        IsAccessible = station.Accessible,
        IsTerminus = station.IsTerminus,
        FareZone = station.FareZone ?? 0
    };

    private static TrainLine ToTrainLine(ILineColumns line) => new()
    {
        Id = line.LineId,
        NameShort = line.LineNameShort,
        NameLong = line.LineName,
        Color = line.ColorHex,
        TextColor = line.TextColorHex,
        TimetablePdf = GetSchedulePDF(line.LineNameShort),
        IsElectrified = line.Electrified,
        PrimaryMover = line.PrimaryMover ?? string.Empty
    };


    private static Uri GetSchedulePDF(string lineName)
    {
        switch (lineName)
        {
            case "BNSF":
                return new Uri("https://schedules.metrarail.com/pdf/BNSF.pdf");
                break;
            case "HC":
                return new Uri("https://schedules.metrarail.com/pdf/HC.pdf");
                break;
            case "MD-N":
                return new Uri("https://schedules.metrarail.com/pdf/MD-N.pdf");
                break;
            case "MD-W":
                return new Uri("https://schedules.metrarail.com/pdf/MD-W.pdf");
                break;
            case "NCS":
                return new Uri("https://schedules.metrarail.com/pdf/NCS.pdf");
                break;
            case "RI":
                return new Uri("https://schedules.metrarail.com/pdf/RI.pdf");
                break;
            case "RI-BEV":
                return new Uri("https://mangogravy.com/RI-Beverly-Branch.pdf");
                break;
            case "SWS":
                return new Uri("https://schedules.metrarail.com/pdf/SWS.pdf");
                break;
            case "UP-N":
                return new Uri("https://schedules.metrarail.com/pdf/UP-N.pdf");
                break;
            case "UP-NW":
                return new Uri("https://schedules.metrarail.com/pdf/UP-NW.pdf");
                break;
            case "UP-NW-MCH":
                return new Uri("https://mangogravy.com/UP-NW-McHenry.pdf");
                break;
            case "UP-W":
                return new Uri("https://schedules.metrarail.com/pdf/UP-W.pdf");
                break;
            default:
                return new Uri("https://schedules.metrarail.com/pdf/ME.pdf");
            
        }
    }

    private static TrainAdvisory ToTrainAdvisory(AlertRow alert, IReadOnlyDictionary<string, TrainLine> linesByRoute) => new()
    {
        Id = alert.AlertId,
        PostedLine = alert.StaticRouteId is null ? null : linesByRoute.GetValueOrDefault(alert.StaticRouteId),
        Header = alert.HeaderText,
        Description = alert.DescriptionText ?? string.Empty,
        Posted = DateTimeOffset.FromUnixTimeSeconds(alert.FirstSeen).UtcDateTime
    };

    private IDbConnection OpenStatic() => Open(_dbConnection);

    private IDbConnection OpenRealtime() => Open(_realtimeDbConnection);

    private static IDbConnection Open(IDbConnectionFactory factory)
    {
        var connection = factory.CreateConnection();
        connection.Open();
        return connection;
    }

    private interface ILineColumns
    {
        int LineId { get; }
        string LineName { get; }
        string LineNameShort { get; }
        string ColorHex { get; }
        string TextColorHex { get; }
        string? ScheduleUrl { get; }
        bool Electrified { get; }
        string? PrimaryMover { get; }
    }

    private interface IStationColumns
    {
        int StationId { get; }
        string StationName { get; }
        string Identifier { get; }
        double Lat { get; }
        double Lon { get; }
        int? FareZone { get; }
        bool Accessible { get; }
        bool IsTerminus { get; }
    }
    
    private sealed class LineRow : ILineColumns
    {
        public int LineId { get; set; }
        public string StaticRouteId { get; set; } = null!;
        public string Identifier { get; set; } = null!;
        public string LineName { get; set; } = null!;
        public string LineNameShort { get; set; } = null!;
        public string ColorHex { get; set; } = null!;
        public string TextColorHex { get; set; } = null!;
        public string? ScheduleUrl { get; set; }
        public bool Electrified { get; set; }
        public string? PrimaryMover { get; set; }
    }

    private sealed class StationRow : IStationColumns
    {
        public int StationId { get; set; }
        public string StationName { get; set; } = null!;
        public string Identifier { get; set; } = null!;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int? FareZone { get; set; }
        public bool Accessible { get; set; }
        public bool IsTerminus { get; set; }
    }

    private sealed class RunRow : ILineColumns
    {
        public int RunId { get; set; }
        public string RunNumber { get; set; } = null!;
        public string? Headsign { get; set; }
        public bool IsOutbound { get; set; }
        public int ServiceClass { get; set; }
        public bool IsSpecial { get; set; }
        public bool HasCenterBoarding { get; set; }
        public bool HasFlagStops { get; set; }
        public bool BikesAllowed { get; set; }
        public int LineId { get; set; }
        public string StaticRouteId { get; set; } = null!;
        public string Identifier { get; set; } = null!;
        public string LineName { get; set; } = null!;
        public string LineNameShort { get; set; } = null!;
        public string ColorHex { get; set; } = null!;
        public string TextColorHex { get; set; } = null!;
        public string? ScheduleUrl { get; set; }
        public bool Electrified { get; set; }
        public string? PrimaryMover { get; set; }
    }

    private sealed class StopRow : IStationColumns
    {
        public int RunKey { get; set; }
        public int StopId { get; set; }
        public int StopNumber { get; set; }
        public int DepartureSeconds { get; set; }
        public bool HasNotice { get; set; }
        public bool CenterBoarding { get; set; }
        public bool SouthBoarding { get; set; }
        public bool BikesAllowed { get; set; }
        public bool FlagStop { get; set; }
        public bool NoPickup { get; set; }
        public int StationId { get; set; }
        public string StationName { get; set; } = null!;
        public string Identifier { get; set; } = null!;
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int? FareZone { get; set; }
        public bool Accessible { get; set; }
        public bool IsTerminus { get; set; }
    }

    private sealed class AlertRow
    {
        public int AlertId { get; set; }
        public string? StaticRouteId { get; set; }
        public string HeaderText { get; set; } = null!;
        public string? DescriptionText { get; set; }
        public long FirstSeen { get; set; }
    }
}
