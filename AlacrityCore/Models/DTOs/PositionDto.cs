namespace AlacrityCore.Models.DTOs;
public record PositionDto
{
    public long InstrumentId { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
}
