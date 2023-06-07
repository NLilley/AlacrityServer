using AlacrityCore.Models.ReqRes.Positions;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("positions")]
public class PositionsController : ControllerBase
{
    private readonly IPositionsFrontService _positionsFrontService;
    public PositionsController(IPositionsFrontService positionsFrontService)
        => (_positionsFrontService) = (positionsFrontService);

    [HttpGet]
    public async Task<GetPositionsResponse> GetPositions([FromQuery]GetPositionsRequest request)
        => new()
        {
            Positions = await _positionsFrontService.GetPositions(this.GetClientId())
        };
}
