using AlacrityCore.Enums;
using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.Back;

internal record Trade
{
    public long TradeId { get; set; }
    public long InstrumentId { get; set; }
    public long ClientId { get; set; }
    public long OrderId { get; set; }
    public DateTime TradeDate { get; set; }
    public TradeDirection TradeDirection { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? Profit { get; set; }

    public TradeDto ToDto() => new()
    {
        TradeId = TradeId,
        InstrumentId = InstrumentId,
        OrderId = OrderId,
        TradeDate = TradeDate,
        Quantity = Quantity,
        TradeDirection = TradeDirection,
        Price = Price,
        Profit = Profit
    };
}
