CREATE TABLE IF NOT EXISTS instruments (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    instrument_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    ticker              TEXT NOT NULL,
    name                TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS instruments_ticker ON instruments(ticker);
CREATE INDEX IF NOT EXISTS instruments_name ON instruments(name);

CREATE TRIGGER IF NOT EXISTS instruments_updated AFTER UPDATE ON instruments
BEGIN
    UPDATE instruments 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE instrument_id = NEW.instrument_id;
END;