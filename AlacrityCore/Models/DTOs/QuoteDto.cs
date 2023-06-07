namespace AlacrityCore.Models.DTOs;
public record QuoteDto
{
    public long InstrumentId { get; set; }
    public decimal? Bid { get; set; }
    public decimal? BidSize { get; set; }
    public decimal? Ask { get; set; }
    public decimal? AskSize { get; set; }
}
