using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Instruments;
public record GetInstrumentResponse
{
    public InstrumentDto Instrument { get; set; }
}
