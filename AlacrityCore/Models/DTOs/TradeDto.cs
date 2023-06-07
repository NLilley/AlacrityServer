using AlacrityCore.Enums;

namespace AlacrityCore.Models.DTOs;
public record TradeDto
{
    public long TradeId { get; set; }
    public long InstrumentId { get; set; }
    public long OrderId { get; set; }
    public DateTime TradeDate { get; set; }
    public TradeDirection TradeDirection { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal? Profit { get; set; }
}
