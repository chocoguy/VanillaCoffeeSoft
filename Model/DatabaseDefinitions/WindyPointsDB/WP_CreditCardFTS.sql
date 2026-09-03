CREATE VIRTUAL TABLE WP_CreditCardFTS
    USING fts5(
                  Name,
                  content='WP_CreditCard',
                  content_rowid='CreditCardId'
);

-- WP_CreditCard has no write path through the API (rows are edited directly in
-- the database), so triggers keep the external-content index in sync instead of
-- the repository maintaining it manually like AnimeFTSReal.
CREATE TRIGGER WP_CreditCardFTS_AfterInsert
    AFTER INSERT ON WP_CreditCard
BEGIN
    INSERT INTO WP_CreditCardFTS(rowid, Name)
    VALUES (new.CreditCardId, new.Name);
END;

CREATE TRIGGER WP_CreditCardFTS_AfterDelete
    AFTER DELETE ON WP_CreditCard
BEGIN
    INSERT INTO WP_CreditCardFTS(WP_CreditCardFTS, rowid, Name)
    VALUES ('delete', old.CreditCardId, old.Name);
END;

CREATE TRIGGER WP_CreditCardFTS_AfterUpdate
    AFTER UPDATE ON WP_CreditCard
BEGIN
    INSERT INTO WP_CreditCardFTS(WP_CreditCardFTS, rowid, Name)
    VALUES ('delete', old.CreditCardId, old.Name);
    INSERT INTO WP_CreditCardFTS(rowid, Name)
    VALUES (new.CreditCardId, new.Name);
END;

-- Index any rows that existed before the FTS table was created.
INSERT INTO WP_CreditCardFTS(WP_CreditCardFTS) VALUES ('rebuild');
