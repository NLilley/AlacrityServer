using AlacrityCore.Database;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityIntegrationTests;
[SetUpFixture]
public class IntegrationFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SqlMapper.AddTypeHandler(new DateTimeHandler());
    }

    public static async Task<SqliteConnection> SetUpDatabaseWithPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix must be not-null");
        }

        var executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        // Bundle initial schemas into local DB
        var rootDatabasePath = Path.Join(executableDir, "Database");
        var prefixPath = Path.Join(executableDir, "TestDatabases", prefix);
        var prefixDatabasePath = Path.Join(prefixPath, "Database");

        var structurePath = "DataBase/Data/alacrity.db";
        var prefixDataPath = Path.Join(prefixPath, structurePath);
        var prefixDataDirectory = Path.GetDirectoryName(prefixDataPath);

        // Sanity Check
        if (!prefixDataDirectory.Contains("AlacrityIntegrationTests"))
            throw new ArgumentException("Test path probably corrupted");

        if (Directory.Exists(prefixDataDirectory))
            Directory.Delete(prefixDataDirectory, true);

        Directory.CreateDirectory(prefixDataDirectory);

        // Copy all folder structure and metadata across
        foreach(var directory in Directory.GetDirectories(rootDatabasePath))
        {
            var name = Path.GetFileName(directory);
            Directory.CreateDirectory(Path.Join(prefixDatabasePath, name));
        }

        foreach (string newPath in Directory.GetFiles(rootDatabasePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(rootDatabasePath, prefixDatabasePath), true);
        }

        DatabaseSetup.Setup(prefixDataPath);

        return new SqliteConnection(DatabaseSetup.GetConnectionStringFromPath(prefixDataPath));
    }
}
