using AlacrityCore.Models.DTOs;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface ISearchQuery
{
    Task<List<InstrumentBriefDto>> SearchInstruments(string searchTerm);
}

internal class SearchQuery : ISearchQuery
{
    private readonly SqliteConnection _connection;
    public SearchQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<List<InstrumentBriefDto>> SearchInstruments(string searchTerm)
        => _connection.Query<InstrumentBriefDto>(
            @"SELECT
                i.instrument_id         InstrumentId,
                i.ticker                Ticker,
                i.name                  Name,
                im.icon_path            IconPath,                
                (
                    CASE WHEN lower(i.ticker) LIKE '%' || lower(@SearchTerm) || '%' THEN 100 ELSE 0 END
                    + CASE WHEN lower(i.name) LIKE '%' || lower(@SearchTerm) || '%' THEN 100 ELSE 0 END
                    + CASE WHEN lower(im.display_name) LIKE '%' || lower(@SearchTerm) || '%' THEN 50 ELSE 0 END
                ) SearchScore
            FROM instruments i
            LEFT JOIN instrument_metadata im ON im.instrument_id = i.instrument_id
            WHERE SearchScore > 0
            ORDER BY SearchScore DESC
            ",
            new { SearchTerm = searchTerm }
        ).ToList();
}
