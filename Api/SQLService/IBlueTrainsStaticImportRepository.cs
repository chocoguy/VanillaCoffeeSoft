using System.Data;
using Api.Gtfs;
using Api.Helpers;
using Dapper;

namespace Api.SQLService;

public interface IBlueTrainsStaticImportRepository
{
    Task<DateTime?> GetLastAppliedPublishTimeAsync();
    Task<StaticLookups> GetLookupsAsync();
    Task ImportAsync(GtfsImportModel model, DateTime publishTimeUtc);
}

public class BlueTrainsStaticImportRepository : IBlueTrainsStaticImportRepository
{
    private const string PublishedMetaKey = "MetraStaticPublishedUtc";
    
    private const string EnsureMetaSql =
        """
        CREATE TABLE IF NOT EXISTS Meta (
            Key   TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        );
        """;
    
    private const string CreateRunOnDateViewSql =
        """
        CREATE VIEW RunOnDate AS
        SELECT  rd.Date,
                r.RunId, r.StaticTripId, r.RunNumber, r.Headsign,
                r.IsOutbound, r.ServiceClass, r.IsSpecial,
                l.Identifier AS LineIdentifier, l.StaticRouteId
        FROM    RunDate rd
                    JOIN    Run     r ON r.RunId   = rd.RunKey
                    JOIN    Line    l ON l.LineId  = r.LineKey;
        """;

    private readonly IBlueTrainsStaticDbConnectionFactory _dbConnection;
    private readonly ILogger<BlueTrainsStaticImportRepository> _logger;

    public BlueTrainsStaticImportRepository(IBlueTrainsStaticDbConnectionFactory dbConnection,
        ILogger<BlueTrainsStaticImportRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<DateTime?> GetLastAppliedPublishTimeAsync()
    {
        using var connection = OpenConnection();
        await EnsureSchemaAsync(connection, null);

        var value = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT Value FROM Meta WHERE Key = @Key;", new { Key = PublishedMetaKey });

        return DateTimeHelper.ParseFlexible(value);
    }

    public async Task<StaticLookups> GetLookupsAsync()
    {
        using var connection = OpenConnection();

        var lines = await connection.QueryAsync<(string Identifier, int LineId)>(
            "SELECT Identifier, LineId FROM Line;");
        var stations = await connection.QueryAsync<(string Identifier, int StationId)>(
            "SELECT Identifier, StationId FROM Station;");
        var shapes = await connection.QueryAsync<(string StaticShapeId, int ShapeId)>(
            "SELECT StaticShapeId, ShapeId FROM Shape;");

        return new StaticLookups(
            lines.ToDictionary(l => l.Identifier, l => l.LineId),
            stations.ToDictionary(s => s.Identifier, s => s.StationId),
            shapes.ToDictionary(s => s.StaticShapeId, s => s.ShapeId));
    }

    public async Task ImportAsync(GtfsImportModel model, DateTime publishTimeUtc)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        await EnsureSchemaAsync(connection, transaction);
        
        await connection.ExecuteAsync(
            "DELETE FROM Stop; DELETE FROM RunDate; DELETE FROM Run;", transaction: transaction);
        
        const string insertRunSql =
            """
            INSERT INTO Run (StaticTripId, LineKey, ShapeKey, RunNumber, Headsign,
                             IsOutbound, ServiceClass, IsSpecial, HasCenterBoarding, HasFlagStops, BikesAllowed)
            VALUES (@StaticTripId, @LineKey, @ShapeKey, @RunNumber, @Headsign,
                    @IsOutbound, @ServiceClass, @IsSpecial, @HasCenterBoarding, @HasFlagStops, @BikesAllowed)
            RETURNING RunId;
            """;

        var runDateRows = new List<object>(model.RunDateCount);
        var stopRows = new List<object>(model.StopCount);

        foreach (var run in model.Runs)
        {
            var runId = await connection.QuerySingleAsync<int>(insertRunSql, new
            {
                run.StaticTripId,
                run.LineKey,
                run.ShapeKey,
                run.RunNumber,
                run.Headsign,
                run.IsOutbound,
                run.ServiceClass,
                run.IsSpecial,
                run.HasCenterBoarding,
                run.HasFlagStops,
                run.BikesAllowed
            }, transaction);

            foreach (var date in run.Dates)
                runDateRows.Add(new { RunKey = runId, Date = date });

            foreach (var stop in run.Stops)
            {
                stopRows.Add(new
                {
                    RunKey = runId,
                    stop.StationKey,
                    stop.Sequence,
                    stop.DepartureSeconds,
                    stop.HasNotice,
                    stop.CenterBoarding,
                    stop.SouthBoarding,
                    stop.BikesAllowed,
                    stop.FlagStop,
                    stop.NoPickup
                });
            }
        }
        
        await connection.ExecuteAsync(
            "INSERT INTO RunDate (RunKey, Date) VALUES (@RunKey, @Date);",
            runDateRows, transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO Stop (RunKey, StationKey, Sequence, DepartureSeconds, HasNotice,
                              CenterBoarding, SouthBoarding, BikesAllowed, FlagStop, NoPickup)
            VALUES (@RunKey, @StationKey, @Sequence, @DepartureSeconds, @HasNotice,
                    @CenterBoarding, @SouthBoarding, @BikesAllowed, @FlagStop, @NoPickup);
            """,
            stopRows, transaction);
        
        await connection.ExecuteAsync(
            """
            INSERT INTO Meta (Key, Value) VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """,
            new { Key = PublishedMetaKey, Value = DateTimeHelper.ToRfc3339(publishTimeUtc) },
            transaction);

        transaction.Commit();

        _logger.LogInformation(
            "BlueTrains static import committed: {Runs} runs, {RunDates} run-dates, {Stops} stops",
            model.Runs.Count, runDateRows.Count, stopRows.Count);
    }
    
    private static async Task EnsureSchemaAsync(IDbConnection connection, IDbTransaction? transaction)
    {
        await connection.ExecuteAsync(EnsureMetaSql, transaction: transaction);

        var columns = (await connection.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('Run');", transaction: transaction)).ToHashSet();

        if (columns.Count == 0)
            return;

        if (!columns.Contains("ServiceClass"))
            await connection.ExecuteAsync(
                "ALTER TABLE Run ADD COLUMN ServiceClass INTEGER NOT NULL DEFAULT 0;",
                transaction: transaction);

        if (!columns.Contains("IsSpecial"))
            await connection.ExecuteAsync(
                "ALTER TABLE Run ADD COLUMN IsSpecial INTEGER NOT NULL DEFAULT 0;",
                transaction: transaction);
        
        await connection.ExecuteAsync("DROP VIEW IF EXISTS RunOnDate;", transaction: transaction);

        if (columns.Contains("IsExpress"))
            await connection.ExecuteAsync("ALTER TABLE Run DROP COLUMN IsExpress;", transaction: transaction);

        await connection.ExecuteAsync(CreateRunOnDateViewSql, transaction: transaction);
    }

    private IDbConnection OpenConnection()
    {
        var connection = _dbConnection.CreateConnection();
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");
        return connection;
    }
}
