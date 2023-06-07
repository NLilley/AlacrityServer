using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;

namespace AlacrityCore.Models.DTOs;
public record OrderDto
{
    public long OrderId { get; set; }
    public long InstrumentId { get; set; }
    public OrderKind OrderKind { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public TradeDirection OrderDirection {get;set;}
    public decimal? LimitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal Filled { get; set; }
}
