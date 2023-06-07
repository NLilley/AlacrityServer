using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.PriceHistory;
public record GetPriceHistoryResponse
{
   public PriceHistoryDto PriceHistory { get; set; }
}
