using AlacrityCore.Enums.PriceHistory;
using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Services.Back;

internal interface IPriceHistoryBackService
{
    Task AddCandle(long instrumentId, CandleTimePeriod period, CandleDto candle);
}

internal class PriceHistoryBackService : IPriceHistoryBackService
{
    private readonly IPriceHistoryQuery _query;
    public PriceHistoryBackService(IPriceHistoryQuery query)
        => (_query) = (query);

    public async Task AddCandle(long instrumentId, CandleTimePeriod period, CandleDto candle)
        => await _query.AddCandle(instrumentId, period, candle);
}
