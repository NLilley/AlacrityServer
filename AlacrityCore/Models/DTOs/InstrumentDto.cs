namespace AlacrityCore.Models.DTOs;
public class InstrumentDto
{
    public long InstrumentId { get; set; }
    public string Ticker { get; set; }
    public string Name { get; set; }

    // From Metadata
    public string DisplayName { get; set; }
    public string Sector { get; set; }
    public string IconPath { get; set; }
    public string Synopsis { get; set; }

    public decimal PreviousClose { get; set; }
}
