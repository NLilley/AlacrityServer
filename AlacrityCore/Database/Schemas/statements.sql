CREATE TABLE IF NOT EXISTS statements (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    statement_id        INTEGER PRIMARY KEY AUTOINCREMENT,
    client_id           INTEGER,
    statement_kind      INTEGER,
    statement           BLOB,

    FOREIGN KEY (client_id) REFERENCES clients (client_id)
);

CREATE INDEX IF NOT EXISTS statements_client_id ON statements (client_id);

CREATE TRIGGER IF NOT EXISTS statements_updated AFTER UPDATE ON statements
BEGIN
    UPDATE statements 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE statement_id = NEW.statement_id;
END;
