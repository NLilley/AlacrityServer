namespace AlacrityCore.Models.ReqRes.Watchlists;
public record AddToWatchlistRequest
{
    public long WatchlistId { get; set; }
    public long InstrumentId { get; set; }
    public long Rank { get; set; }
}
