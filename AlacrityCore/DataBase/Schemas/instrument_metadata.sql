CREATE TABLE IF NOT EXISTS instrument_metadata (
    created_date                TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date                 TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    instrument_metadata_id      INTEGER PRIMARY KEY AUTOINCREMENT,
    instrument_id               INTEGER NOT NULL,
    display_name                TEXT NOT NULL,
    sector                      TEXT NOT NULL,
    icon_path                   TEXT NOT NULL,
    synopsis                    TEXT NOT NULL,

    FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS instrument_metadata_instrument_id ON instrument_metadata(instrument_id);

CREATE TRIGGER IF NOT EXISTS instrument_metadata_updated AFTER UPDATE ON instrument_metadata
BEGIN
    UPDATE instrument_metadata 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE instrument_metadata_id = NEW.instrument_metadata_id;
END;