using AlacrityCore.Models.ReqRes.PriceHistory;

namespace AlacrityCore.Services.Front;
public interface IPriceHistoryFrontService
{
    public Task<GetPriceHistoryResponse> GetPriceHistory(GetPriceHistoryRequest request);
}

internal class PriceHistoryFrontService : IPriceHistoryFrontService
{
    private readonly IPriceHistoryQuery _query;
    public PriceHistoryFrontService(IPriceHistoryQuery query)
        => (_query) = (query);

    public async Task<GetPriceHistoryResponse> GetPriceHistory(GetPriceHistoryRequest request)
        => new()
        {
            PriceHistory = await _query.GetPriceHistory(
                request.InstrumentId,
                request.Start,
                request.End,
                request.Period
            )
        };
}
