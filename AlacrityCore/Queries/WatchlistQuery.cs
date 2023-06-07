using AlacrityCore.Models.DTOs;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IWatchlistQuery
{
    Task<bool> DoesWatchlistBelongsToClient(long clientId, long watchlistId);
    Task<bool> DoesWatchlistItemBelongToClient(long clientId, long watchlistItemId);

    Task<List<WatchlistDTO>> GetWatchlists(long clientId);
    Task<long> AddWatchlist(long clientId, string name);
    Task<long> AddToWatchlist(long watchlistId, long instrumentId, long rank);
    Task UpdateWatchlistName(long watchlistId, string name);
    Task DeleteWatchlist(long watchlistId);
    Task DeleteWatchlistItem(long watchlistItemId);
}

internal class WatchlistQuery : IWatchlistQuery
{
    private readonly SqliteConnection _connection;
    public WatchlistQuery(SqliteConnection connection) => (_connection) = (connection);

    public async Task<List<WatchlistDTO>> GetWatchlists(long clientId)
    {
        var watchlists = _connection.Query<WatchlistDTO>(
            @"
            SELECT 
                watchlist_id        AS WatchlistId, 
                name                AS Name
            FROM watchlists 
            WHERE client_id = @ClientId
            ORDER BY watchlist_id",
            new { ClientId = clientId }
        ).ToList();

        var items = _connection.Query<WatchlistItemDto>(
            @"
            SELECT
                watchlist_item_id   AS WatchlistItemId,
                watchlist_id        AS WatchlistId,
                instrument_id       AS InstrumentId, 
                rank                AS Rrder
            FROM watchlist_items
            WHERE watchlist_id IN @Ids",
            new { Ids = watchlists.Select(w => w.WatchlistId).ToArray() }
        ).ToList();

        foreach (var item in items)
        {
            var watchlist = watchlists.First(w => w.WatchlistId == item.WatchlistId);
            if (watchlist == null)
                continue;

            watchlist.WatchlistItems.Add(item);
        }

        return watchlists;
    }

    public async Task<long> AddWatchlist(long clientId, string name)
    {
        // Unique constraint will prevent duplicates
        return _connection.ExecuteScalar<long>(
            "INSERT INTO watchlists (client_id, name) VALUES (@ClientId, @Name) RETURNING watchlist_id",
            new { ClientId = clientId, Name = name }
        );
    }

    public async Task<long> AddToWatchlist(long watchlistId, long instrumentId, long rank)
    {
        return _connection.ExecuteScalar<long>(
            @"
            INSERT INTO watchlist_items 
            (watchlist_id, instrument_id, rank) 
            VALUES 
            (@WatchlistId, @InstrumentId, @Rank)
            RETURNING watchlist_item_id",
            new
            {
                WatchlistId = watchlistId,
                InstrumentId = instrumentId,
                Rank = rank
            }
        );
    }

    public async Task UpdateWatchlistName(long watchlistId, string name)
        => _connection.Execute(
            "UPDATE watchlists SET name = @Name WHERE watchlist_id = @WatchlistId",
            new { Name = name, WatchlistId = watchlistId }
        );


    public async Task DeleteWatchlist(long watchlistId)
        => _connection.Execute(
            "DELETE FROM watchlists WHERE watchlist_id = @WatchlistId",
            new { WatchlistId = watchlistId }
        );


    public async Task DeleteWatchlistItem(long watchlistItemId)
        => _connection.Execute(
            "DELETE FROM watchlist_items WHERE watchlist_item_id = @WatchlistItemId",
            new { WatchlistItemId = watchlistItemId }
        );

    public async Task<bool> DoesWatchlistBelongsToClient(long clientId, long watchlistId)
    {
        var ownerClientId = _connection.Query<int?>(
            "SELECT client_id FROM watchlists WHERE watchlist_id = @WatchlistId",
            new { WatchlistId = watchlistId }
        ).FirstOrDefault();

        return ownerClientId == clientId;
    }

    public async Task<bool> DoesWatchlistItemBelongToClient(long clientId, long watchlistItemId)
    {
        var ownerClientId = _connection.Query<int?>(
            @"SELECT w.client_id 
            FROM watchlist_items wi
            INNER JOIN watchlists w ON w.watchlist_id = wi.watchlist_id
            WHERE wi.watchlist_item_id = @WatchlistItemId",
            new { WatchlistItemId = watchlistItemId }
        ).FirstOrDefault();

        return ownerClientId == clientId;
    }
}
