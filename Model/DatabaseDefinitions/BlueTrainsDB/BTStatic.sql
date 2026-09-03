-- =====================================================================
--  BlueTrains — static schedule schema  (static.db)
--
--  Built from the Metra GTFS static feed. This database is READ-ONLY at
--  runtime: a feed refresh builds a new file and swaps it in, so nothing
--  here is written to while the API is serving.
--
--  Refresh cadence: WEEKLY. Metra issues short-lived calendar periods
--  (e.g. 20260810-20260816) and re-letters them on each publish, so
--  trip ids are NOT stable across feed versions.
-- =====================================================================

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

-- ---------------------------------------------------------------------
-- Line — a user-facing line. One row per GTFS route, PLUS the synthetic
-- branch lines. StaticRouteId is the parent GTFS route_id and is what
-- the realtime feed reports, so several Lines can share one StaticRouteId
-- (ME, ME-BI and ME-SC all map back to route_id 'ME').
-- ---------------------------------------------------------------------
CREATE TABLE Line (
                      LineId          INTEGER PRIMARY KEY,
                      StaticRouteId   TEXT    NOT NULL,          -- routes.txt route_id, e.g. 'ME'
                      Name            TEXT    NOT NULL,          -- 'Metra Electric — Blue Island'
                      NameShort       TEXT    NOT NULL,
                      Identifier      TEXT    NOT NULL UNIQUE,   -- 'ME-BI'  (our key, not Metra's)
                      ColorHex        TEXT    NOT NULL,
                      TextColorHex    TEXT    NOT NULL,
                      ScheduleUrl     TEXT,
                      Electrified     INTEGER NOT NULL DEFAULT 0 CHECK (Electrified IN (0,1)),
                      PrimaryMover    TEXT
);

CREATE INDEX ix_line_route ON Line (StaticRouteId);

-- ---------------------------------------------------------------------
-- Station — stops.txt. Lat/Lon are numeric so they can drive distance
-- and snap-to-shape maths.
--
-- NOTE: display names are NOT unique. Three stations are called some
-- form of "95th St." (95TH-UP, LONGWOOD, 95TH-BEV) and there is a
-- "Prairie Crossing" / "Prairie Crossing." pair. Always carry Identifier.
-- ---------------------------------------------------------------------
CREATE TABLE Station (
                         StationId       INTEGER PRIMARY KEY,
                         Name            TEXT    NOT NULL,
                         Identifier      TEXT    NOT NULL UNIQUE,   -- stops.txt stop_id, e.g. 'AURORA'
                         Lat             REAL    NOT NULL,
                         Lon             REAL    NOT NULL,
                         FareZone        INTEGER,                   -- stops.txt zone_id
                         Accessible      INTEGER NOT NULL DEFAULT 0 CHECK (Accessible IN (0,1)),
                         IsTerminus      INTEGER NOT NULL DEFAULT 0 CHECK (IsTerminus IN (0,1))
);

CREATE INDEX ix_station_geo ON Station (Lat, Lon);

-- ---------------------------------------------------------------------
-- Shape — one row per shapes.txt shape_id, points collapsed into a single
-- encoded polyline. 31 rows instead of 7,385.
--
-- StaticShapeId is TEXT, not int: the ids look like 'BNSF_IB_1'.
-- ---------------------------------------------------------------------
CREATE TABLE Shape (
                       ShapeId         INTEGER PRIMARY KEY,
                       StaticShapeId   TEXT    NOT NULL UNIQUE,   -- 'UP-NW_IB_2'
                       PolyLine        TEXT    NOT NULL,          -- encoded polyline, precision 5
                       PointCount      INTEGER NOT NULL DEFAULT 0,
                       LengthMeters    REAL                       -- cumulative, for progress maths
);

-- ---------------------------------------------------------------------
-- Run — one scheduled train, as a TEMPLATE (no date). Corresponds 1:1
-- with a trips.txt row.
--
-- RunNumber is TEXT: most are numeric ('702') but the feed also carries
-- 'UNRAV01' (Ravinia specials), 'MX01'/'MX02' (SWS extras) and 'BV..'.
--
-- RollingStockLevel has NO source in GTFS — Metra publishes no carriage
-- or occupancy data. Populate it manually or leave it NULL.
--
-- ServiceClass is derived, not from the feed: coverage = stops / stations
-- PASSED between the run's own endpoints, measured against the corridor for
-- its (Line, direction, day type). Thresholds 0.95 / 0.70 / 0.45 give
-- 0 Local, 1 Express, 2 Limited Express, 3 Super Express.
-- Measuring against stations PASSED (not stations its same-origin/destination
-- peers serve) is what stops a lone short-turn express scoring 1.0 and
-- landing in Local.
--
-- IsSpecial is an ORTHOGONAL flag, not a class: a run either carries a
-- non-numeric RunNumber (Ravinia 'RAV1', SWS 'MX01') or never touches a
-- downtown terminal (CUS, OTC, MILLENNIUM, LSS). Such runs still get a
-- ServiceClass — a Ravinia extra is also an Express.
-- ---------------------------------------------------------------------
CREATE TABLE Run (
                     RunId               INTEGER PRIMARY KEY,
                     StaticTripId        TEXT    NOT NULL UNIQUE,  -- 'UP-NW_UNW702_V2_B' — realtime join key
                     LineKey             INTEGER NOT NULL REFERENCES Line  (LineId),
                     ShapeKey            INTEGER          REFERENCES Shape (ShapeId),
                     RunNumber           TEXT    NOT NULL,
                     Headsign            TEXT,
                     IsOutbound          INTEGER NOT NULL DEFAULT 0 CHECK (IsOutbound IN (0,1)),
                     ServiceClass        INTEGER NOT NULL DEFAULT 0 CHECK (ServiceClass IN (0,1,2,3)),
                     IsSpecial           INTEGER NOT NULL DEFAULT 0 CHECK (IsSpecial IN (0,1)),
                     RollingStockLevel   INTEGER,
                     HasCenterBoarding   INTEGER NOT NULL DEFAULT 0 CHECK (HasCenterBoarding IN (0,1)),
                     HasFlagStops        INTEGER NOT NULL DEFAULT 0 CHECK (HasFlagStops      IN (0,1)),
                     BikesAllowed        INTEGER NOT NULL DEFAULT 1 CHECK (BikesAllowed      IN (0,1))
);

CREATE INDEX ix_run_line   ON Run (LineKey, RunNumber);
CREATE INDEX ix_run_number ON Run (RunNumber);

-- ---------------------------------------------------------------------
-- RunDate — the materialised calendar. calendar.txt weekday bitmasks
-- expanded to explicit dates, with calendar_dates.txt exceptions ALREADY
-- APPLIED (type 1 adds, type 2 removes; removals win).
--
-- Regression case: 2026-08-16 (Sunday). Weekday matching alone yields
-- B2, B4, C2 and C4; the exceptions drop B2/B4, leaving C2/C4 only.
-- Getting this wrong duplicates every Sunday train.
-- ---------------------------------------------------------------------
CREATE TABLE RunDate (
                         RunDateId       INTEGER PRIMARY KEY,
                         RunKey          INTEGER NOT NULL REFERENCES Run (RunId) ON DELETE CASCADE,
                         Date            TEXT    NOT NULL,          -- 'YYYY-MM-DD'
                         UNIQUE (RunKey, Date)
);

-- The workhorse index: every departures query enters through a date.
CREATE INDEX ix_rundate_date ON RunDate (Date, RunKey);

-- ---------------------------------------------------------------------
-- Stop — one call at one station by one Run. (stop_times.txt)
--
-- DepartureSeconds is seconds since the START OF THE SERVICE DAY and
-- MAY EXCEED 86400 — this feed has 2,710 rows past midnight, up to
-- 26:25:00. Compose a real instant as RunDate.Date + DepartureSeconds.
-- Never store this as a time-of-day.
--
-- arrival_time == departure_time on all 75,676 rows in this feed, which
-- is why there is no Arrival column. Re-check if that ever changes.
--
-- FlagStop  <- pickup_type/drop_off_type = 3  (flag stop)
-- NoPickup  <- pickup_type = 1                (drop-off only, 28 rows)
-- ---------------------------------------------------------------------
CREATE TABLE Stop (
                      StopId              INTEGER PRIMARY KEY,
                      RunKey              INTEGER NOT NULL REFERENCES Run     (RunId) ON DELETE CASCADE,
                      StationKey          INTEGER NOT NULL REFERENCES Station (StationId),
                      Sequence            INTEGER NOT NULL,      -- NON-CONTIGUOUS: sort by it, never index with it
                      DepartureSeconds    INTEGER NOT NULL CHECK (DepartureSeconds BETWEEN 0 AND 108000),
                      HasNotice           INTEGER NOT NULL DEFAULT 0 CHECK (HasNotice      IN (0,1)),
                      CenterBoarding      INTEGER NOT NULL DEFAULT 0 CHECK (CenterBoarding IN (0,1)),
                      SouthBoarding       INTEGER NOT NULL DEFAULT 0 CHECK (SouthBoarding  IN (0,1)),
                      BikesAllowed        INTEGER NOT NULL DEFAULT 1 CHECK (BikesAllowed   IN (0,1)),
                      FlagStop            INTEGER NOT NULL DEFAULT 0 CHECK (FlagStop       IN (0,1)),
                      NoPickup            INTEGER NOT NULL DEFAULT 0 CHECK (NoPickup       IN (0,1)),
                      UNIQUE (RunKey, Sequence)
);

-- Departure boards: "what leaves this station in this window".
CREATE INDEX ix_stop_station ON Stop (StationKey, DepartureSeconds);
CREATE INDEX ix_stop_run     ON Stop (RunKey, Sequence);

-- ---------------------------------------------------------------------
-- Meta — sync bookkeeping written by BlueTrainsStaticSyncService.
-- 'MetraStaticPublishedUtc' holds the published.txt timestamp (RFC 3339
-- UTC) of the last successfully applied import; written in the same
-- transaction as the import so the gate can never drift from the data.
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Meta (
                     Key    TEXT PRIMARY KEY,
                     Value  TEXT NOT NULL
);

-- ---------------------------------------------------------------------
-- Convenience view: runs resolved to a concrete date.
-- ---------------------------------------------------------------------
CREATE VIEW RunOnDate AS
SELECT  rd.Date,
        r.RunId, r.StaticTripId, r.RunNumber, r.Headsign,
        r.IsOutbound, r.ServiceClass, r.IsSpecial,
        l.Identifier AS LineIdentifier, l.StaticRouteId
FROM    RunDate rd
            JOIN    Run     r ON r.RunId   = rd.RunKey
            JOIN    Line    l ON l.LineId  = r.LineKey;
