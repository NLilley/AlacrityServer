using AlacrityCore.Enums.PriceHistory;
using AlacrityCore.Models.DTOs;
using Dapper;
using Microsoft.Data.Sqlite;

internal interface IPriceHistoryQuery
{
    public Task<PriceHistoryDto> GetPriceHistory(long instrumentId, DateTime start, DateTime end, CandleTimePeriod period);
    public Task<CandleDto> GetLatestCandle(long instrumentId);
    Task AddCandle(long instrumentId, CandleTimePeriod period, CandleDto candle);
    Task DeleteOldCandles(DateTime oldThan);
}

internal class PriceHistoryQuery : IPriceHistoryQuery
{
    private readonly SqliteConnection _connection;
    public PriceHistoryQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<PriceHistoryDto> GetPriceHistory(long instrumentId, DateTime start, DateTime end, CandleTimePeriod period)
    {
        var priceHistory = new PriceHistoryDto
        {
            InstrumentId = instrumentId,
            Start = start,
            End = end
        };

        var candles = _connection.Query<CandleDto>(
            @"SELECT 
                price_date          AS Date,
                open                AS Open, 
                high                AS High, 
                low                 AS Low, 
                close               AS Close
              FROM price_history
              WHERE 
                instrument_id = @InstrumentId
                AND time_period = @TimePeriod
                AND datetime(price_date) >= @Start
                AND datetime(price_date) < @End
              ORDER BY price_date",
            new { InstrumentId = instrumentId, TimePeriod = period, Start = start, End = end }
        ).ToList();

        priceHistory.Data = candles;
        return priceHistory;
    }

    public async Task<CandleDto> GetLatestCandle(long instrumentId)
        => _connection.Query<CandleDto>(
            @"SELECT
                price_date          AS Date,
                open                AS Open,
                high                AS High,
                low                 AS Low,
                close               AS Close
            FROM price_history
            WHERE instrument_id = @InstrumentId
            ORDER BY price_date DESC
            LIMIT 1",
            new { InstrumentId = instrumentId }
        ).FirstOrDefault();

    public async Task AddCandle(long instrumentId, CandleTimePeriod period, CandleDto candle)
        => _connection.Query(
            @"INSERT INTO price_history 
            (instrument_id, time_period, price_date, open, high, low, close)
            VALUES
                (@InstrumentId, @TimePeriod, @PriceDate, @Open, @High, @Low, @Close)
                ON CONFLICT (instrument_id, time_period, price_date) DO UPDATE SET 
                    open = @Open,
                    high = @High,
                    low = @Low,
                    close = @Close
",
            new
            {
                InstrumentId = instrumentId,
                TimePeriod = period,
                PriceDate = candle.Date,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close
            }
    );

    public async Task DeleteOldCandles(DateTime olderThan)
        => _connection.Query(
            @"DELETE FROM price_history WHERE created_date < @Date",
            new
            {
                Date = olderThan
            }
        );
}