using AlacrityCore.Models.ReqRes.PriceHistory;
using AlacrityCore.Services.Front;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("pricehistory")]
public class PriceHistoryController : ControllerBase
{
    private readonly IPriceHistoryFrontService _priceHistoryFrontService;
    public PriceHistoryController(IPriceHistoryFrontService priceHistoryFrontService)
        => (_priceHistoryFrontService) = (priceHistoryFrontService);

    [HttpGet]
    public async Task<GetPriceHistoryResponse> GetPriceHistory([FromQuery] GetPriceHistoryRequest request)
        => await _priceHistoryFrontService.GetPriceHistory(request);
}
