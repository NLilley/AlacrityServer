CREATE TABLE IF NOT EXISTS client_settings (
    created_date        TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    client_setting_id   INTEGER PRIMARY KEY AUTOINCREMENT,
    client_id           INTEGER NOT NULL,
    name                TEXT NOT NULL,
    value               TEXT,

    FOREIGN KEY (client_id) REFERENCES clients (client_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS client_settings_client_id_name ON client_settings(client_id, name);

CREATE TRIGGER IF NOT EXISTS client_settings_updated AFTER UPDATE ON client_settings
BEGIN
    UPDATE client_settings 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE client_setting_id = NEW.client_setting_id;
END;
