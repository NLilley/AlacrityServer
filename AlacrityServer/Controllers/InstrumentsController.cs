using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes.Instruments;
using AlacrityCore.Services.Front;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("instruments")]
public class InstrumentsController : ControllerBase
{
    private readonly IInstrumentFrontService _instrumentsFrontService;
    public InstrumentsController(IInstrumentFrontService instrumentsFrontService)
        => (_instrumentsFrontService) = (instrumentsFrontService);

    [HttpGet]
    public async Task<GetInstrumentsResponse> GetInstruments()
        => new()
        {
            Instruments = await _instrumentsFrontService.GetInstruments()
        };

    [HttpGet("{instrumentId}")]
    public async Task<GetInstrumentResponse> GetInstrument([FromRoute] GetInstrumentRequest request)
        => new()
        {
            Instrument = await _instrumentsFrontService.GetInstrument(request.InstrumentId)
        };
}
