using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Database;

public static class DatabaseSetup
{
    public static string GetConnectionStringFromPath(string dbPath)
        => $"Data Source={dbPath};Pooling=True;foreign keys=true";

    public static void Setup(string dbPath)
    {
        var fullPath = Path.GetFullPath(dbPath);
        var dataDirectory = Path.GetDirectoryName(fullPath);
        var databaseDirectory = Path.GetFullPath(Path.Join(dataDirectory, "../"));
        Directory.CreateDirectory(dataDirectory);

        var connectionString = GetConnectionStringFromPath(dbPath);
        var connection = new SqliteConnection(connectionString);
        connection.Execute("PRAGMA journal_mode = 'wal'");

        var meta = connection.Query<string>(@"
            SELECT name FROM sqlite_master WHERE type='table' AND name='_db_metadata'
        ").ToList();

        if (meta.Count == 0)
        {
            Console.WriteLine("DATABASE NOT FOUND!");

            BackupExistingData(dbPath);
            CreateDatabase(connection, databaseDirectory);
        }
    }

    private static void BackupExistingData(string dbPath)
    {
        var dbDirectory = Path.GetDirectoryName(dbPath);

        Console.WriteLine("Creating Backup of lingering DB artifacts");

        var backupPath = Path.Join(dbDirectory, "Backups");
        Directory.CreateDirectory(backupPath);

        var newBackupDirectory = Path.Join(backupPath, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
        Directory.CreateDirectory(newBackupDirectory);
        foreach (var file in Directory.EnumerateFiles(dbDirectory))
        {
            if (Directory.Exists(file))
                continue;

            var fileName = Path.GetFileName(file);
            File.Copy(file, newBackupDirectory + $"/{fileName}");
        }

        Console.WriteLine("Backup Complete");
    }

    private static void CreateDatabase(SqliteConnection connection, string databasePath)
    {
        Console.WriteLine($"Creating a new database with connection string: {connection.ConnectionString}");

        var schemas = new[]
        {
            "_db_metadata.sql",

            "instruments.sql",
            "instrument_metadata.sql",
            "instrument_indicators.sql",
            "price_history.sql",

            "web_message_users.sql",
            "clients.sql",
            "client_settings.sql",
            "logon_history.sql",
            "web_messages.sql",
            "watchlists.sql",
            "watchlist_items.sql",
            "statements.sql",

            "ledger.sql",
            "positions.sql",

            "trades.sql",
            "orders.sql"
        };

        var initialData = new[]
        {
            "instruments_data.sql",
            "instrument_metadata_data.sql",

            "web_message_users_data.sql",
            "web_messages_data.sql",

            "clients_data.sql",

            "statements_data.sql",

            "orders_data.sql",
            "trades_data.sql",
            "ledger_data.sql",
            "positions_data.sql",

            "watchlists_data.sql",
            "watchlist_items_data.sql",            
        };

        foreach (var schema in schemas)
        {
            var sql = File.ReadAllText(Path.Join(databasePath, $"Schemas/{schema}"));
            connection.Execute(sql);
        }

        foreach (var datum in initialData)
        {
            var sql = File.ReadAllText(Path.Join(databasePath, $"./InitialData/{datum}"));
            connection.Execute(sql);
        }

        connection.Execute("INSERT INTO _db_metadata (version) VALUES ('v1.0.0')");
        Console.WriteLine("Finished Creating database");
    }
}

