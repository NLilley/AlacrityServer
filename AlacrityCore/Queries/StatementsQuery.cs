using AlacrityCore.Models.DTOs;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IStatementsQuery
{
    Task<List<StatementDto>> GetStatements(int clientId);
}

internal class StatementsQuery : IStatementsQuery
{
    private readonly SqliteConnection _connection;
    public StatementsQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<List<StatementDto>> GetStatements(int clientId)
        => _connection.Query<StatementDto>(
            @"SELECT
                statement_id                AS StatementId,
                statement_kind              AS StatementKind,
                CAST(statement AS BLOB)     AS Statement
            FROM statements s
            WHERE s.client_id = @ClientId",
            new { ClientId = clientId }
        ).ToList();
}
