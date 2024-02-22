CREATE TABLE instrument_indicators (
	created_date                TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date                 TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
	instrument_indicator_id		INTEGER PRIMARY KEY AUTOINCREMENT,
	instrument_id				INTEGER,
	indicator_kind				INTEGER,
	name						TEXT,
	value						REAL,
	FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id)
);

CREATE INDEX instrument_indicators_instrument_id ON instrument_indicators (instrument_id);
CREATE INDEX instrument_indicators_indicator_kind ON instrument_indicators (indicator_kind);
CREATE UNIQUE INDEX instrument_indicators_id_kind_name ON instrument_indicators (instrument_id, indicator_kind, name);

CREATE TRIGGER IF NOT EXISTS instrument_indicators_updated AFTER UPDATE ON instrument_indicators
BEGIN
    UPDATE instrument_indicators 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE instrument_indicator_id = NEW.instrument_indicator_id;
END;
