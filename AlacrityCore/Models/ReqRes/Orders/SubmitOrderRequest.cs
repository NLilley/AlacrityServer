using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;

namespace AlacrityCore.Models.ReqRes.Orders;
public record SubmitOrderRequest
{
    public long InstrumentId { get; set; }
    public decimal Quantity { get; set; }
    public OrderKind OrderKind { get; set; }
    public TradeDirection OrderDirection {get;set;}
    public decimal? LimitPrice { get; set; }
}
