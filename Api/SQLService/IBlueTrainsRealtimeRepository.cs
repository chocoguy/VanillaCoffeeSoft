using System.Data;
using Dapper;
using Model.BlueTrains;

namespace Api.SQLService;

public record AlertSyncResult(int Applied, int Cleared);

public interface IBlueTrainsRealtimeRepository
{
    Task<AlertSyncResult> SyncAlertsAsync(IReadOnlyList<Alert> alerts, long observedAt);
}

public class BlueTrainsRealtimeRepository : IBlueTrainsRealtimeRepository
{
    private const string UpsertAlertSql =
        """
        INSERT INTO Alert (FeedEntityId, StaticRouteId, AgencyId, HeaderText, DescriptionText, Url,
                           Cause, Effect, ContentHash, FirstSeen, LastSeen)
        VALUES (@FeedEntityId, @StaticRouteId, @AgencyId, @HeaderText, @DescriptionText, @Url,
                @Cause, @Effect, @ContentHash, @FirstSeen, @LastSeen)
        ON CONFLICT(FeedEntityId) DO UPDATE SET
            StaticRouteId   = excluded.StaticRouteId,
            AgencyId        = excluded.AgencyId,
            HeaderText      = excluded.HeaderText,
            DescriptionText = excluded.DescriptionText,
            Url             = excluded.Url,
            Cause           = excluded.Cause,
            Effect          = excluded.Effect,
            ContentHash     = excluded.ContentHash,
            LastSeen        = excluded.LastSeen,
            EditedAt        = CASE WHEN Alert.ContentHash <> excluded.ContentHash
                                   THEN excluded.LastSeen ELSE Alert.EditedAt END,
            FirstSeen       = CASE WHEN Alert.ClearedAt IS NOT NULL
                                   THEN excluded.FirstSeen ELSE Alert.FirstSeen END,
            ClearedAt       = NULL;
        """;
    
    private const string ClearMissingAlertsSql =
        """
        UPDATE Alert
           SET ClearedAt = @ObservedAt
         WHERE ClearedAt IS NULL
           AND LastSeen < @ObservedAt;
        """;

    private readonly IBlueTrainsRealtimeDbConnectionFactory _dbConnection;
    private readonly ILogger<BlueTrainsRealtimeRepository> _logger;

    public BlueTrainsRealtimeRepository(IBlueTrainsRealtimeDbConnectionFactory dbConnection,
        ILogger<BlueTrainsRealtimeRepository> logger)
    {
        _dbConnection = dbConnection;
        _logger = logger;
    }

    public async Task<AlertSyncResult> SyncAlertsAsync(IReadOnlyList<Alert> alerts, long observedAt)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        var applied = alerts.Count == 0
            ? 0
            : await connection.ExecuteAsync(UpsertAlertSql, alerts, transaction);

        var cleared = await connection.ExecuteAsync(ClearMissingAlertsSql,
            new { ObservedAt = observedAt }, transaction);

        transaction.Commit();

        _logger.LogInformation("BlueTrains alert sync committed: {Applied} live, {Cleared} cleared", applied, cleared);

        return new AlertSyncResult(applied, cleared);
    }

    private IDbConnection OpenConnection()
    {
        var connection = _dbConnection.CreateConnection();
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");
        return connection;
    }
}
