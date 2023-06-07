CREATE TABLE IF NOT EXISTS ledger (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    ledger_id           INTEGER PRIMARY KEY AUTOINCREMENT,
    client_id           INTEGER NOT NULL,
    instrument_id       INTEGER NOT NULL,
    transaction_kind    INTEGER,
    quantity            REAL,
    
    FOREIGN KEY (client_id) REFERENCES clients (client_id),
    FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id)
);

CREATE INDEX IF NOT EXISTS ledger_client_id ON ledger (client_id);
CREATE INDEX IF NOT EXISTS ledger_instrument_id ON ledger (instrument_id);

CREATE TRIGGER IF NOT EXISTS ledger_updated AFTER UPDATE ON ledger
BEGIN
    UPDATE ledger 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE ledger_id = NEW.ledger_id;
END;
