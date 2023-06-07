namespace AlacrityCore.Models.DTOs;
public class PriceHistoryDto
{
    public long InstrumentId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public List<CandleDto> Data { get; set; }
}
