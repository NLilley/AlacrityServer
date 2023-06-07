using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Instruments;
public record GetInstrumentsResponse
{
    public List<InstrumentBriefDto> Instruments { get; set; }
}
