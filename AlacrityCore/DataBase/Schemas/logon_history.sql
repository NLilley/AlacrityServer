CREATE TABLE IF NOT EXISTS logon_history (
	created_date                TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date                 TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
	logon_history_id			INTEGER PRIMARY KEY AUTOINCREMENT,
	client_id					INTEGER,

	FOREIGN KEY (client_id) REFERENCES clients(client_id)
);

CREATE INDEX IF NOT EXISTS logon_history_client_id ON logon_history(client_id);

CREATE TRIGGER IF NOT EXISTS logon_history_updated AFTER UPDATE ON logon_history
BEGIN
    UPDATE logon_history
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE client_id = NEW.client_id;
END;