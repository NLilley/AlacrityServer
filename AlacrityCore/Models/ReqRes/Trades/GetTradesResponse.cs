using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Trades;
public record GetTradesResponse
{
    public List<TradeDto> Trades { get; set; }
}
