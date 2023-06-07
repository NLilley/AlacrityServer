using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Watchlists;
public record GetWatchlistResponse
{
    public List<WatchlistDTO> Watchlists { get; set; }
}
