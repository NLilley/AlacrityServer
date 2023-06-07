using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using Microsoft.Extensions.Logging;

namespace AlacrityCore.Services.Front;
public interface IWatchlistsFrontService
{
    Task<List<WatchlistDTO>> GetWatchlists(long clientId);
    Task<long?> AddWatchlist(long clientId, string name);
    Task<long?> AddToWatchlist(long clientId, long watchlistId, long instrumentId, long rank);
    Task<bool> UpdateWatchlistName(long clientId, long watchlistId, string name);
    Task<bool> DeleteWatchlist(long clientId, long watchlistId);
    Task<bool> DeleteWatchlistItem(long clientId, long watchlistItemId);
}

internal class WatchlistsFrontService : IWatchlistsFrontService
{
    private readonly ILogger<WatchlistsFrontService> _logger;
    private readonly IWatchlistQuery _query;
    public WatchlistsFrontService(ILogger<WatchlistsFrontService> logger, IWatchlistQuery query)
        => (_logger, _query) = (logger, query);

    public async Task<List<WatchlistDTO>> GetWatchlists(long clientId)
        => await _query.GetWatchlists(clientId);

    public async Task<long?> AddWatchlist(long clientId, string name)
        => await _query.AddWatchlist(clientId, name);

    public async Task<long?> AddToWatchlist(long clientId, long watchlistId, long instrumentId, long rank)
    {
        var ownsWatchlist = await _query.DoesWatchlistBelongsToClient(clientId, watchlistId);
        if (!ownsWatchlist)
        {
            _logger.LogError("Client: {clientId} tried to add to watchlist: {watchlistId} which does not belong to it",
                 clientId, watchlistId);
            return null;
        }        

        try
        {
            return await _query.AddToWatchlist(watchlistId, instrumentId, rank);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to Add to Watchlist");
            return null;
        }
    }

    public async Task<bool> UpdateWatchlistName(long clientId, long watchlistId, string name)
    {
        var ownsWatchlist = await _query.DoesWatchlistBelongsToClient(clientId, watchlistId);
        if (!ownsWatchlist)
        {
            _logger.LogError("Client: {clientId} tried to update the name of watchlist: {watchlistId} which does not belong to it",
                 clientId, watchlistId);
            return false;
        }

        try
        {
            await _query.UpdateWatchlistName(watchlistId, name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to Update Watchlist Name");
            return false;
        }
    }

    public async Task<bool> DeleteWatchlist(long clientId, long watchlistId)
    {
        var ownsWatchlist = await _query.DoesWatchlistBelongsToClient(clientId, watchlistId);
        if (!ownsWatchlist)
        {
            _logger.LogError("Client: {clientId} tried to delete watchlist: {watchlistId} which does not belong to it",
                 clientId, watchlistId);
            return false;
        }

        try
        {
            await _query.DeleteWatchlist(watchlistId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to Delete Watchlist");
            return false;
        }

    }

    public async Task<bool> DeleteWatchlistItem(long clientId, long watchlistItemId)
    {
        var ownsWatchlistItem = await _query.DoesWatchlistItemBelongToClient(clientId, watchlistItemId);
        if (!ownsWatchlistItem)
        {
            _logger.LogError("Client: {clientId} tried to delete watchlist item: {watchlistItemId} which does not belong to it",
                    clientId, watchlistItemId);
            return false;
        }

        try
        {
            await _query.DeleteWatchlistItem(watchlistItemId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to Delete Watchlist Item");
            return false;
        }
    }
}
