CREATE TABLE IF NOT EXISTS price_history (
    created_date            TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date             TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    price_history_id        INTEGER PRIMARY KEY AUTOINCREMENT,    
    instrument_id           INTEGER NOT NULL,
    time_period             INTEGER NOT NULL,
    price_date              TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    open                    REAL,
    high                    REAL,
    low                     REAL,
    close                   REAL,
    FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id)
);

CREATE INDEX IF NOT EXISTS price_history_instrument_id ON price_history(instrument_id);
CREATE INDEX IF NOT EXISTS price_history_time_period ON price_history(time_period);
CREATE UNIQUE INDEX IF NOT EXISTS price_history_instrument_period_date ON price_history(instrument_id, time_period, price_date);

CREATE TRIGGER IF NOT EXISTS price_history_updated AFTER UPDATE ON price_history
BEGIN
    UPDATE price_history 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE price_history_id = NEW.price_history_id;
END;
