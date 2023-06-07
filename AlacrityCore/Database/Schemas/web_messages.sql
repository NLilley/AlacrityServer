CREATE TABLE IF NOT EXISTS web_messages (
    created_date            TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    edited_date             TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    web_message_id          INTEGER PRIMARY KEY AUTOINCREMENT,
    root_message_id         INTEGER,
    owner_id                INTEGER NOT NULL,
    to_id                   INTEGER NOT NULL,
    from_id                 INTEGER NOT NULL,
    message_kind            INTEGER NOT NULL,
    title                   TEXT,
    message                 TEXT,
    read                    INTEGER NOT NULL DEFAULT 0,
    finalized               INTEGER NOT NULL DEFAULT 0,

    FOREIGN KEY (root_message_id) REFERENCES web_messages (web_message_id),
    FOREIGN KEY (owner_id) REFERENCES web_message_users (web_message_user_id)
    FOREIGN KEY (to_id) REFERENCES web_message_users (web_message_user_id)
    FOREIGN KEY (from_id) REFERENCES web_message_users (web_message_user_id)
);

CREATE INDEX IF NOT EXISTS web_message_root_message_id ON web_messages (root_message_id);
CREATE INDEX IF NOT EXISTS web_message_owner_id ON web_messages (owner_id);
CREATE INDEX IF NOT EXISTS web_message_to_id ON web_messages (to_id);
CREATE INDEX IF NOT EXISTS web_message_from_id ON web_messages (from_id);

CREATE TRIGGER IF NOT EXISTS web_messages_inserted AFTER INSERT ON web_messages
BEGIN
    UPDATE web_messages 
    SET root_message_id = COALESCE(root_message_id, web_message_id)
    WHERE web_message_id = NEW.web_message_id;
END;

CREATE TRIGGER IF NOT EXISTS web_messages_updated AFTER UPDATE ON web_messages
BEGIN
    UPDATE web_messages 
    SET edited_date = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE web_message_id = NEW.web_message_id;
END;
