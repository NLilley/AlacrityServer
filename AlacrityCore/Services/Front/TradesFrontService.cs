using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Front;
public interface ITradesFrontService
{
    Task<List<TradeDto>> GetTrades(int clientId);
}

internal class TradesFrontService : ITradesFrontService
{
    private readonly ITradesQuery _query;
    public TradesFrontService(ITradesQuery query)
        => (_query) = (query);

    public async Task<List<TradeDto>> GetTrades(int clientId)
        => (await _query.GetTrades(clientId)).Select(t => t.ToDto()).ToList();
}
