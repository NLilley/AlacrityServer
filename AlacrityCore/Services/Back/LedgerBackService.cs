using AlacrityCore.Enums;
using AlacrityCore.Models.Back;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Back;
internal interface ILedgerBackService
{
    Task<List<LedgerEntry>> GetLedgerEntries(int clientId);
    Task AddEntry(int clientId, long instrumentId, TransactionKind kind, decimal quantity);
}

internal class LedgerBackService : ILedgerBackService
{
    private readonly ILedgerQuery _query;
    public LedgerBackService(ILedgerQuery query)
        => (_query) = (query);

    public async Task AddEntry(int clientId, long instrumentId, TransactionKind kind, decimal quantity)
        => await _query.AddEntry(clientId, instrumentId, kind, quantity);

    public async Task<List<LedgerEntry>> GetLedgerEntries(int clientId)
        => await _query.GetLedgerEntries(clientId);
}
