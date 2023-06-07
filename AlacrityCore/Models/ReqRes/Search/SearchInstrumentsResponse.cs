using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Search;
public record SearchInstrumentsResponse
{
    public List<InstrumentBriefDto> Instruments { get; set; }
}
