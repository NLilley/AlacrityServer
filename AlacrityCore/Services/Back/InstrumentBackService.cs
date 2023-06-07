using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Back;
internal interface IInstrumentBackService
{
    Task UpsertInstrumentIndicator(InstrumentIndicatorDto indicator);
}

internal class InstrumentBackService : IInstrumentBackService
{
    private readonly IInstrumentsQuery _query;
    public InstrumentBackService(IInstrumentsQuery query)
        => (_query) = (query);

    public Task UpsertInstrumentIndicator(InstrumentIndicatorDto indicator)
        => _query.UpsertInstrumentIndicator(indicator);
}
