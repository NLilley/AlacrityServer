CREATE TABLE IF NOT EXISTS web_message_users (
	created_date                TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date                 TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
	web_message_user_id			INTEGER PRIMARY KEY AUTOINCREMENT,
	name						TEXT
);

CREATE INDEX IF NOT EXISTS web_message_users_name ON web_message_users(name);

CREATE TRIGGER IF NOT EXISTS web_message_users_updated AFTER UPDATE ON web_message_users
BEGIN
    UPDATE web_message_users 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now') 
    WHERE web_message_user_id = NEW.web_message_user_id;
END;
