CREATE TABLE IF NOT EXISTS watchlist_items (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    watchlist_item_id   INTEGER PRIMARY KEY AUTOINCREMENT,
    watchlist_id        INTEGER,
    instrument_id       INTEGER,
    rank                INTEGER,

    FOREIGN KEY (watchlist_id) REFERENCES watchlists (watchlist_id) ON DELETE CASCADE,
    FOREIGN KEY (instrument_id) REFERENCES instruments (instrument_id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS watchlist_items_watchlist_id_instrument_id ON watchlist_items (watchlist_id, instrument_id);
CREATE INDEX IF NOT EXISTS watchlist_items_instrument_id ON watchlist_items (instrument_id);


CREATE TRIGGER IF NOT EXISTS watchlist_items_updated AFTER UPDATE ON watchlist_items
BEGIN
    UPDATE watchlist_items
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE transaction_id = NEW.transaction_id;
END;