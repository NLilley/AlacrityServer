namespace AlacrityCore.Models.DTOs;

public record WatchlistDTO
{
    public long WatchlistId { get; set; }
    public string Name { get; set; }

    public List<WatchlistItemDto> WatchlistItems { get; set; } = new();
}
