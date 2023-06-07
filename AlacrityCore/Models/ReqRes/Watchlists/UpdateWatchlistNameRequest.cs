namespace AlacrityCore.Models.ReqRes.Watchlists;
public record UpdateWatchlistNameRequest
{
    public long WatchlistId { get; set; }
    public string Name { get; set; }
}
