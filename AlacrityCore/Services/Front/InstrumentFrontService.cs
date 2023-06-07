using AlacrityCore.Enums;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;

namespace AlacrityCore.Services.Front;

public interface IInstrumentFrontService
{
    Task<List<InstrumentBriefDto>> GetInstruments();
    Task<InstrumentDto> GetInstrument(long instrumentId);
    Task<QuoteDto> GetQuote(long instrumentId);
    Task<Dictionary<string, InstrumentIndicatorDto>> GetIndicators(long instrumentId);
}

internal class InstrumentFrontService : IInstrumentFrontService
{
    private readonly IInstrumentsQuery _query;
    private readonly IExchange _exchange;
    public InstrumentFrontService(
        IInstrumentsQuery query,
        IExchange exchange
    ) => (_query, _exchange) = (query, exchange);

    public async Task<List<InstrumentBriefDto>> GetInstruments()
        => (await _query.GetInstruments()).Where(i => i.InstrumentId != (long)SpecialInstruments.Cash).ToList();

    public async Task<InstrumentDto> GetInstrument(long instrumentId)
    {
        if (instrumentId == (long)SpecialInstruments.Cash)
            throw new ArgumentException("Cannot fetch 'Cash' instrument data");

        return await _query.GetInstrument(instrumentId);
    }

    public async Task<QuoteDto> GetQuote(long instrumentId)
    {
        if (instrumentId == (long)SpecialInstruments.Cash)
            throw new ArgumentException("Cannot fetch 'Cash' instrument data");

        return await _exchange.GetQuote(instrumentId);
    }

    public async Task<Dictionary<string, InstrumentIndicatorDto>> GetIndicators(long instrumentId)
    {
        if (instrumentId == (long)SpecialInstruments.Cash)
            throw new ArgumentException("Cannot fetch 'Cash' instrument data");

        return await _query.GetIndicators(instrumentId);
    }
}
