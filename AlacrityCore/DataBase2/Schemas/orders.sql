CREATE TABLE IF NOT EXISTS orders (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    order_id            INTEGER PRIMARY KEY AUTOINCREMENT,
    instrument_id       INTEGER,
    client_id           INTEGER,
    order_date          TEXT NOT NULL,
    order_kind          INTEGER,
    order_status        INTEGER,
    order_direction     INTEGER,
    limit_price         REAL,
    quantity            REAL,
    filled              REAL,

    FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id),
    FOREIGN KEY (client_id) REFERENCES clients (client_id)
);

CREATE INDEX IF NOT EXISTS orders_instrument_id ON orders(instrument_id);
CREATE INDEX IF NOT EXISTS orders_client_id ON orders(client_id);
CREATE INDEX IF NOT EXISTS orders_order_date ON orders(order_date);

CREATE TRIGGER IF NOT EXISTS orders_updated AFTER UPDATE ON orders
BEGIN
    UPDATE orders 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE order_id = NEW.order_id;
END;
