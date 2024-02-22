CREATE TABLE IF NOT EXISTS clients (
    created_date                TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    edited_date                 TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    client_id                   INTEGER PRIMARY KEY AUTOINCREMENT,
    username                    TEXT NOT NULL,    
    hashed_password             TEXT NOT NULL, -- Note: Salt will be stored as part of hash
    email                       TEXT NOT NULL,
    web_message_user_id         INTEGER,
    first_name                  TEXT NOT NULL,
    other_names                 TEXT NOT NULL,

    FOREIGN KEY (web_message_user_id) REFERENCES web_message_users (web_message_user_id)
);

CREATE UNIQUE INDEX IF NOT EXISTS clients_username ON clients(username);
CREATE UNIQUE INDEX IF NOT EXISTS clients_web_message_user_id ON clients(web_message_user_id);

CREATE TRIGGER IF NOT EXISTS clients_updated AFTER UPDATE ON clients
BEGIN
    UPDATE clients
    SET edited_date = CURRENT_TIMESTAMP 
    WHERE client_id = NEW.client_id;
END;
