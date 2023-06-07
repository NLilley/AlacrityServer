CREATE TABLE IF NOT EXISTS positions (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    position_id         INTEGER PRIMARY KEY AUTOINCREMENT,
    client_id           INTEGER,    
    instrument_id       INTEGER,
    quantity            REAL NOT NULL,
    average_price       REAL NOT NULL,
    summed_quantities   REAL NOT NULL,
    FOREIGN KEY (client_id) REFERENCES clients (client_id),
    FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id)
);

CREATE INDEX IF NOT EXISTS positions_client_id ON positions(client_id);
CREATE INDEX IF NOT EXISTS positions_instrument_id ON positions(instrument_id);
CREATE UNIQUE INDEX IF NOT EXISTS positions_client_id_instrument_id ON positions(client_id, instrument_id);

CREATE TRIGGER IF NOT EXISTS positions_updated AFTER UPDATE ON positions
BEGIN
    UPDATE positions 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE position_id = NEW.position_id;
END;

CREATE TRIGGER IF NOT EXISTS positions_clean_up_null_positions AFTER UPDATE ON positions
BEGIN
    DELETE FROM positions
    WHERE position_id = NEW.position_id AND NEW.quantity = 0;
END;

