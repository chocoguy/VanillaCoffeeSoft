-- Migration 001: rewrite Anime date columns to canonical RFC 3339 UTC ("yyyy-MM-ddTHH:mm:ssZ").
-- Handles legacy Microsoft.Data.Sqlite text: "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss",
-- "yyyy-MM-dd HH:mm:ss.FFFFFFF" (all stored as UTC wall-clock). NULLs untouched.
-- Idempotent: canonical values re-format to themselves. The strftime(...) IS NOT NULL
-- guard leaves any unparseable value as-is instead of nulling it out.
-- AirTime is a broadcast time-of-day string and is intentionally NOT migrated.

BEGIN TRANSACTION;

UPDATE Anime SET OnAir        = strftime('%Y-%m-%dT%H:%M:%SZ', OnAir)
WHERE OnAir        IS NOT NULL AND strftime('%Y-%m-%dT%H:%M:%SZ', OnAir)        IS NOT NULL;

UPDATE Anime SET OffAir       = strftime('%Y-%m-%dT%H:%M:%SZ', OffAir)
WHERE OffAir       IS NOT NULL AND strftime('%Y-%m-%dT%H:%M:%SZ', OffAir)       IS NOT NULL;

UPDATE Anime SET LastSynced   = strftime('%Y-%m-%dT%H:%M:%SZ', LastSynced)
WHERE LastSynced   IS NOT NULL AND strftime('%Y-%m-%dT%H:%M:%SZ', LastSynced)   IS NOT NULL;

UPDATE Anime SET LastModified = strftime('%Y-%m-%dT%H:%M:%SZ', LastModified)
WHERE LastModified IS NOT NULL AND strftime('%Y-%m-%dT%H:%M:%SZ', LastModified) IS NOT NULL;

COMMIT;
