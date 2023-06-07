using AlacrityCore.Models.DTOs;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IPositionsQuery
{
    Task<List<PositionDto>> GetPositions(int clientId);
    Task<(decimal quantity, decimal averagePrice)> AddToPosition(int clientId, long instrumentId, decimal quantity, decimal tradedPrice);
}

internal class PositionsQuery : IPositionsQuery
{
    private readonly SqliteConnection _connection;
    public PositionsQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<List<PositionDto>> GetPositions(int clientId)
        => _connection.Query<PositionDto>(
            @"SELECT                
                instrument_id               AS InstrumentId,
                quantity                    AS Quantity,
                average_price               AS AveragePrice
            FROM positions p
            WHERE p.client_id = @ClientId",
            new { ClientId = clientId }
        ).ToList();

    // Note: Potential Race Conditions using this method!
    public async Task<(decimal quantity, decimal averagePrice)> AddToPosition(
        int clientId, long instrumentId, decimal quantity, decimal tradedPrice
    )
    {
        if (quantity == 0)
            throw new ArgumentException("Cannot insert a position change with 0 quantity");

        return _connection.Query<(decimal quantity, decimal averagePrice)>(
            @"INSERT INTO positions (client_id, instrument_id, quantity, average_price, summed_quantities)
              VALUES (@ClientId, @InstrumentId, @Quantity, @TradedPrice, @Quantity)
                ON CONFLICT (client_id, instrument_id) DO UPDATE SET 
                    quantity = quantity + @Quantity,
                    summed_quantities = summed_quantities + @Quantity,
                    average_price = CASE 
                        WHEN SIGN(quantity) != SIGN(@Quantity) THEN average_price -- Note: We currently don't permit Short trades, so this will be correct.
                        ELSE ((average_price * summed_quantities) + (@TradedPrice * @Quantity)) / (summed_quantities + @Quantity)
                    END
              RETURNING quantity, average_price",
            new
            {
                ClientId = clientId,
                InstrumentId = instrumentId,
                Quantity = quantity,
                TradedPrice = tradedPrice
            }
        ).FirstOrDefault();
    }
}
