namespace AlacrityCore.Models.DTOs;
public record InstrumentBriefDto
{
    public long InstrumentId { get; set; }
    public string Ticker { get; set; }
    public string Name { get; set; }
    public string IconPath { get; set; }
}
