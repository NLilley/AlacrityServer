using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.Back;
internal record Order
{
    public long OrderId { get; set; }
    public long InstrumentId { get; set; }
    public int? ClientId { get; set; }
    public DateTime OrderDate {get; set;}
    public OrderKind OrderKind { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public TradeDirection OrderDirection {get;set;}
    public decimal? LimitPrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal Filled { get; set; }

    public OrderDto ToDto() => new()
    {
        OrderId = OrderId,
        InstrumentId = InstrumentId,
        OrderKind = OrderKind,
        OrderStatus = OrderStatus,
        OrderDirection = OrderDirection,
        LimitPrice = LimitPrice,
        Quantity = Quantity,
        Filled = Filled
    };
}
