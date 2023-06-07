CREATE TABLE IF NOT EXISTS trades (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    trade_id            INTEGER PRIMARY KEY AUTOINCREMENT,
    instrument_id       INTEGER,
    client_id           INTEGER,
    order_id            INTEGER,
    trade_date          TEXT NOT NULL,
    trade_direction     INTEGER NOT NULL,
    quantity            REAL NOT NULL,
    price               REAL NOT NULL,
    profit              REAL,

    FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id),
    FOREIGN KEY (client_id) REFERENCES clients (client_id),
    FOREIGN KEY (order_id) REFERENCES orders (order_id)
);

CREATE INDEX IF NOT EXISTS trades_instrument_id ON trades(instrument_id);
CREATE INDEX IF NOT EXISTS trades_client_id ON trades(client_id);
CREATE INDEX IF NOT EXISTS trades_order_id ON trades(order_id);
CREATE INDEX IF NOT EXISTS trades_trade_date ON trades(trade_date);

CREATE TRIGGER IF NOT EXISTS trades_updated AFTER UPDATE ON trades
BEGIN
    UPDATE trades 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE trade_id = NEW.trade_id;
END;
