using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IWebMessageUserQuery
{
    public Task<long> AddWebMessageUser(string name);
}

internal class WebMessageUserQuery : IWebMessageUserQuery
{
    private readonly SqliteConnection _connection;
    public WebMessageUserQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<long> AddWebMessageUser(string name)
        => _connection.ExecuteScalar<long>(
          @"INSERT INTO web_message_users (name) VALUES (@Name) RETURNING web_message_user_id",
            new { Name = name }
        );
}
