using AlacrityCore.Models.DTOs;
using AlacrityCore.Utils;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IInstrumentsQuery
{
    Task<List<InstrumentBriefDto>> GetInstruments();
    Task<InstrumentDto> GetInstrument(long instrumentId);
    Task<Dictionary<string, InstrumentIndicatorDto>> GetIndicators(long instrumentId);
    Task UpsertInstrumentIndicator(InstrumentIndicatorDto indicator);
}

internal class InstrumentsQuery : IInstrumentsQuery
{
    private readonly SqliteConnection _connection;
    public InstrumentsQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<List<InstrumentBriefDto>> GetInstruments()
        => _connection.Query<InstrumentBriefDto>(
            @"SELECT 
                i.instrument_id            InstrumentId, 
                i.ticker                   Ticker,
                i.name                     Name,
                im.icon_path               Iconpath
            FROM instruments i
            LEFT JOIN instrument_metadata im ON im.instrument_id = i.instrument_id"
        ).ToList();

    public async Task<InstrumentDto> GetInstrument(long instrumentId)
    {
        var lastDateYesterday = DateTime.UtcNow.Floor(TimeSpan.FromDays(1));
        var instrument = _connection.Query<InstrumentDto>(
            @"SELECT
                i.instrument_id         InstrumentId,
                i.ticker                Ticker,
                i.name                  Name,
                im.display_name         DisplayName,
                im.sector               Sector,
                im.icon_path            IconPath,
                im.synopsis             Synopsis,
                COALESCE(
                    (                        
                        SELECT close
                        FROM price_history
                        WHERE price_date < @PreviousCloseDate
                        AND instrument_id = @InstrumentId
                        ORDER BY price_date DESC
                        LIMIT 1 -- Take the most recent record from before today                                  
                    ),
                    (
                        SELECT close
                        FROM price_history
                        WHERE instrument_id = @InstrumentId
                        ORDER BY price_date
                        LIMIT 1 -- Or the earliest record we have
                    ),
                    100 -- Dummy Value                    
                )                       PreviousClose             
            FROM instruments i
            LEFT JOIN instrument_metadata im ON im.instrument_id = i.instrument_id
            WHERE i.instrument_id = @InstrumentId",
            new { InstrumentId = instrumentId, PreviousCloseDate = lastDateYesterday }
        ).Single();

        return instrument;
    }

    public async Task<Dictionary<string, InstrumentIndicatorDto>> GetIndicators(long instrumentId)
    {
        var indicators = _connection.Query<InstrumentIndicatorDto>(
            @"SELECT 
                        instrument_id InstrumentId,
                        indicator_kind IndicatorKind, 
                        name Name, 
                        value Value
                      FROM instrument_indicators
                      WHERE instrument_id = @InstrumentId",
            new { InstrumentId = instrumentId }
        ).ToDictionary(i => i.Name);

        return indicators;
    }

    public async Task UpsertInstrumentIndicator(InstrumentIndicatorDto indicator)
        => _connection.Execute(
            @"INSERT INTO instrument_indicators (instrument_id, indicator_kind, name, value)
            VALUES (@InstrumentId, @IndicatorKind, @Name, @Value)
                ON CONFLICT (instrument_id, indicator_kind, name) DO UPDATE SET value = @Value",
            new
            {
                InstrumentId = indicator.InstrumentId,
                IndicatorKind = indicator.IndicatorKind,
                Name = indicator.Name,
                Value = indicator.Value
            }
        );
}

