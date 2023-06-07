using AlacrityCore.Models.Back;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface ITradesQuery
{
    Task<List<Trade>> GetTrades(int clientId);
    Task<long> AddTrade(Trade trade);
}

internal class TradesQuery : ITradesQuery
{
    private readonly SqliteConnection _connection;
    public TradesQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<List<Trade>> GetTrades(int clientId)
        => _connection.Query<Trade>(
            @"SELECT
                trade_id            AS TradeId,
                instrument_id       AS InstrumentId,
                client_id           AS ClientId,
                order_id            AS OrderId,
                trade_date          AS TradeDate,
                trade_direction     AS TradeDirection,
                quantity            AS Quantity,
                price               AS Price,
                profit              AS Profit
            FROM trades t
            WHERE t.client_id = @ClientId
            ORDER BY trade_date DESC
",
            new { ClientId = clientId }
        ).ToList();

    public async Task<long> AddTrade(Trade trade)
        => _connection.ExecuteScalar<long>(
        @"INSERT INTO trades 
            (instrument_id, client_id, order_id, trade_date, trade_direction, quantity, price, profit)
        VALUES 
            (@InstrumentId, @ClientId, @OrderId, @TradeDate, @TradeDirection, @Quantity, @Price, @Profit)
        RETURNING trade_id
        ",
        new
        {            
            InstrumentId = trade.InstrumentId,
            ClientId = trade.ClientId,
            OrderId = trade.OrderId,
            TradeDate = trade.TradeDate,
            TradeDirection = trade.TradeDirection,
            Quantity = trade.Quantity,
            Price = trade.Price,
            Profit = trade.Profit
        }
    );
}
