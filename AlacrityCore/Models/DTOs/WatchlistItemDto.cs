namespace AlacrityCore.Models.DTOs;

public record WatchlistItemDto
{   
    public long WatchlistItemId { get; set; }
    public long WatchlistId { get; set; }
    public long InstrumentId { get; set; }
    public int Rank { get; set; }
}
