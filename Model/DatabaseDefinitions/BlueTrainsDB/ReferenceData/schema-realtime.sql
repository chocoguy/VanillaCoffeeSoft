-- =====================================================================
--  BlueTrains — realtime schema  (realtime.db)
--
--  The ONLY database written to at runtime. Kept separate from static.db
--  so a static feed refresh is a file swap, not a destructive migration.
--
--  Join to static with:
--      ATTACH DATABASE 'static.db' AS s;
--  SQLite cannot enforce foreign keys across attached databases, so the
--  *Key columns below are unenforced hints. The TEXT ids are the source
--  of truth, and they may legitimately reference trips that do not exist
--  in static.db (ADDED trips, or a stale static feed).
--
--  Vehicle positions are deliberately NOT stored — 185 vehicles at a 15s
--  poll is ~890k rows/day that nothing queries. Keep them in memory.
-- =====================================================================

PRAGMA journal_mode = WAL;
PRAGMA synchronous  = NORMAL;   -- safe under WAL; full fsync per commit is overkill
PRAGMA foreign_keys = ON;

-- ---------------------------------------------------------------------
-- Alert — service alerts, with the lifecycle the feed does NOT provide.
--
-- The alerts feed carries no active_period at all, so FirstSeen /
-- EditedAt / ClearedAt exist only because we record them. That is the
-- entire reason this table exists.
--
-- FeedEntityId ('DevAPI-<GUID>') was verified stable across polls —
-- 18 of 21 ids persisted unchanged over 10 minutes, and the 3 that
-- moved were genuine lifecycle events, not id churn. ContentHash is
-- stored anyway so identity can be switched to it without a migration.
--
-- Every alert observed had EXACTLY ONE informed_entity, always
-- agency + route scoped, so the route is denormalised onto this row.
-- If a feed ever carries multiple, this needs a child table — assert
-- informed_entity.Count == 1 at parse time so you find out.
--
-- Cause/Effect are stored but were UNKNOWN_CAUSE(1)/UNKNOWN_EFFECT(8)
-- on all 21 alerts. Do not build UI that switches on them.
-- ---------------------------------------------------------------------
CREATE TABLE Alert (
    AlertId         INTEGER PRIMARY KEY,
    FeedEntityId    TEXT    NOT NULL UNIQUE,   -- 'DevAPI-94EEA091-CE3A-...'
    StaticRouteId   TEXT,                      -- informed_entity.route_id, e.g. 'MD-N'
    AgencyId        TEXT,                      -- informed_entity.agency_id, 'METRA'
    HeaderText      TEXT    NOT NULL,
    DescriptionText TEXT,
    Url             TEXT,
    Cause           INTEGER NOT NULL DEFAULT 1,   -- 1 = UNKNOWN_CAUSE
    Effect          INTEGER NOT NULL DEFAULT 8,   -- 8 = UNKNOWN_EFFECT
    ContentHash     TEXT    NOT NULL,          -- sha256(route || header || description)
    FirstSeen       INTEGER NOT NULL,          -- epoch seconds, ours not the feed's
    LastSeen        INTEGER NOT NULL,
    EditedAt        INTEGER,                   -- bumped when ContentHash changes
    ClearedAt       INTEGER                    -- NULL while live
);

-- Partial index: the hot query only ever touches live alerts, and this
-- keeps it that way as cleared history accumulates.
CREATE INDEX ix_alert_live ON Alert (StaticRouteId) WHERE ClearedAt IS NULL;
CREATE INDEX ix_alert_seen ON Alert (LastSeen);

-- ---------------------------------------------------------------------
-- TripUpdate — one row per trip present in the feed.
--
-- Separate from the stop-level rows because a CANCELED trip arrives with
-- ZERO stop_time_updates; there would be nowhere to record it otherwise.
--
-- TripRelationship (TripDescriptor.ScheduleRelationship):
--   0 SCHEDULED   1 ADDED (deprecated)   2 UNSCHEDULED   3 CANCELED
--   5 REPLACEMENT 6 DUPLICATED           7 DELETED       8 NEW
-- ---------------------------------------------------------------------
CREATE TABLE TripUpdate (
    TripUpdateId        INTEGER PRIMARY KEY,
    StaticTripId        TEXT    NOT NULL,      -- 'BNSF_BN1247_V2_B'
    ServiceDate         TEXT    NOT NULL,      -- 'YYYY-MM-DD' from trip.start_date
    RunKey              INTEGER,               -- static.Run.RunId; NULL if unresolved
    StaticRouteId       TEXT,
    VehicleId           TEXT,                  -- vehicle.id, usually the train number
    TripRelationship    INTEGER NOT NULL DEFAULT 0,
    FeedTimestamp       INTEGER NOT NULL,      -- header.timestamp, epoch seconds
    UpdatedAt           INTEGER NOT NULL,      -- when WE wrote it
    UNIQUE (StaticTripId, ServiceDate)
);

CREATE INDEX ix_tripupdate_run  ON TripUpdate (RunKey);
CREATE INDEX ix_tripupdate_date ON TripUpdate (ServiceDate);

-- ---------------------------------------------------------------------
-- TripUpdateStop — fully RESOLVED prediction for one stop on one trip.
--
-- The feed is sparse: a trip may carry a single stop_time_update whose
-- delay propagates to every LATER stop until another update overrides
-- it. Do that expansion in the POLLER and store one row per stop, so
-- reads are a plain join instead of a window function on every request.
-- Source records which values were inferred that way.
--
-- ScheduleRelationship (StopTimeUpdate.ScheduleRelationship):
--   0 SCHEDULED   1 SKIPPED   2 NO_DATA   3 UNSCHEDULED
--
-- PredictedSeconds is NULL when the feed says NO_DATA — that is
-- "no prediction available", which is NOT the same as "no delay".
-- ---------------------------------------------------------------------
CREATE TABLE TripUpdateStop (
    TripUpdateKey       INTEGER NOT NULL REFERENCES TripUpdate (TripUpdateId) ON DELETE CASCADE,
    StopSequence        INTEGER NOT NULL,
    StaticStopId        TEXT    NOT NULL,      -- 'AURORA'
    StationKey          INTEGER,               -- static.Station.StationId; NULL if unresolved
    PredictedSeconds    INTEGER,               -- service-day seconds; may exceed 86400
    DelaySeconds        INTEGER,
    ScheduleRelationship INTEGER NOT NULL DEFAULT 0,
    Source              INTEGER NOT NULL DEFAULT 0,  -- 0 = from feed, 1 = propagated
    PRIMARY KEY (TripUpdateKey, StopSequence)
) WITHOUT ROWID;

CREATE INDEX ix_tus_stop ON TripUpdateStop (StaticStopId);

-- ---------------------------------------------------------------------
-- Live view: current predictions with the trip context folded in,
-- excluding cancelled trips and skipped stops.
-- ---------------------------------------------------------------------
CREATE VIEW LivePrediction AS
SELECT  tu.StaticTripId, tu.ServiceDate, tu.RunKey, tu.StaticRouteId,
        tu.FeedTimestamp,
        s.StopSequence, s.StaticStopId, s.StationKey,
        s.PredictedSeconds, s.DelaySeconds, s.Source
FROM    TripUpdate     tu
JOIN    TripUpdateStop s ON s.TripUpdateKey = tu.TripUpdateId
WHERE   tu.TripRelationship <> 3      -- not CANCELED
  AND   s.ScheduleRelationship <> 1;  -- not SKIPPED

-- =====================================================================
--  Per-poll write pattern (one transaction, for atomicity — readers must
--  never see half a feed applied):
--
--    BEGIN;
--      -- upsert alerts, then sweep anything untouched this cycle:
--      UPDATE Alert SET ClearedAt = :now
--       WHERE ClearedAt IS NULL AND LastSeen < :thisPoll;
--      -- upsert trip updates + resolved stops
--    COMMIT;
--
--  Only run the clear-sweep if the fetch SUCCEEDED. A failed poll that
--  reaches it will mark every live alert resolved at once.
-- =====================================================================
