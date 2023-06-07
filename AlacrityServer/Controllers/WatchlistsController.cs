using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes.Watchlists;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("watchlists")]
public class WatchlistsController : ControllerBase
{
    public IWatchlistsFrontService _watchlistFrontService;
    public WatchlistsController(IWatchlistsFrontService watchlistsFrontService)
        => (_watchlistFrontService) = (watchlistsFrontService);

    [HttpGet]
    public async Task<GetWatchlistResponse> GetWatchlists([FromQuery] GetWatchlistRequest request)
        => new()
        {
            Watchlists = await _watchlistFrontService.GetWatchlists(this.GetClientId())
        };

    [HttpPost]
    public async Task<AddWatchlistResponse> AddWatchlist([FromBody] AddWatchlistRequest request)
        => new()
        {
            WatchlistId = (await _watchlistFrontService.AddWatchlist(this.GetClientId(), request.Name)).Value
        };

    [HttpPut("item")]
    public async Task<AddToWatchlistResponse> AddToWatchlist([FromBody] AddToWatchlistRequest request)
    {
        var newWatchlistItemResult = await _watchlistFrontService.AddToWatchlist(this.GetClientId(), request.WatchlistId, request.InstrumentId, request.Rank);
        if(newWatchlistItemResult == null)
        {
            throw new ArgumentException("Unable to add item to watchlist");
        }
        
        return new()
        {
            WatchlistItemId = newWatchlistItemResult.Value
        };
    }

    [HttpDelete]
    public async Task<DeleteWatchlistItemResponse> DeleteWatchlist([FromBody] DeleteWatchlistRequest request)
        => new()
        {
            Succeeded = await _watchlistFrontService.DeleteWatchlist(this.GetClientId(), request.WatchlistId)
        };

    [HttpDelete("item")]
    public async Task<DeleteWatchlistItemResponse> DeleteWatchlistItem([FromBody] DeleteWatchlistItemRequest request)
        => new()
        {
            Succeeded = await _watchlistFrontService.DeleteWatchlistItem(this.GetClientId(), request.WatchlistItemId)
        };
}
