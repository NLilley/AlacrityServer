using AlacrityCore.Enums.PriceHistory;

namespace AlacrityCore.Models.ReqRes.PriceHistory;
public record GetPriceHistoryRequest
{
    public long InstrumentId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public CandleTimePeriod Period { get; set; }
}
