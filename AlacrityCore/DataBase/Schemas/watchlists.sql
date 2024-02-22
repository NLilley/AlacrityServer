CREATE TABLE IF NOT EXISTS watchlists (
   created_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
   edited_date          TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
   watchlist_id         INTEGER PRIMARY KEY AUTOINCREMENT,
   client_id            INTEGER NOT NULL,
   name                 TEXT,
    
   FOREIGN KEY (client_id) REFERENCES clients (client_id)
);

CREATE INDEX IF NOT EXISTS watchlists_watchlist_id ON watchlists(watchlist_id);
CREATE UNIQUE INDEX IF NOT EXISTS watchlists_name ON watchlists(client_id, name);

CREATE TRIGGER IF NOT EXISTS watchlists_updated AFTER UPDATE ON watchlists
BEGIN
    UPDATE watchlists 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE watchlist_id = NEW.watchlist_id;
END;