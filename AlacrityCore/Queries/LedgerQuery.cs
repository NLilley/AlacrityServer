using AlacrityCore.Enums;
using AlacrityCore.Models.Back;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface ILedgerQuery
{
    Task<List<LedgerEntry>> GetLedgerEntries(int clientId);
    Task AddEntry(int clientId, long instrumentId, TransactionKind kind, decimal quantity);
}

internal class LedgerQuery : ILedgerQuery
{
    private readonly SqliteConnection _connection;
    public LedgerQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<List<LedgerEntry>> GetLedgerEntries(int clientId)
        => _connection.Query<LedgerEntry>(
            @"SELECT 
                created_date            CreatedDate,
                ledger_id               LedgerId,
                client_id               ClientId,
                instrument_id           InstrumentId,
                transaction_kind        TransactionKind,
                quantity                Quantity
              FROM ledger
              WHERE client_id = @ClientId
              ORDER BY ledger_id DESC",
            new { ClientId = clientId}
        ).ToList();

    public async Task AddEntry(int clientId, long instrumentId, TransactionKind kind, decimal quantity)
        => _connection.Execute(
            @"INSERT INTO ledger (client_id, instrument_id, transaction_kind, quantity)
              VALUES (@ClientId, @InstrumentId, @Kind, @Quantity)",
            new { ClientId = clientId, InstrumentId = instrumentId, Kind = kind, Quantity = quantity }
        );
}
