using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes.Trades;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers
{
    [ApiController]
    [Route("trades")]
    public class TradesController : ControllerBase
    {
        public ITradesFrontService _tradesFrontService;
        public TradesController(ITradesFrontService tradesFrontService)
            => (_tradesFrontService) = (tradesFrontService);

        [HttpGet]
        public async Task<GetTradesResponse> GetTrades([FromQuery]GetTradesRequest request)
            => new()
            {
                Trades = await _tradesFrontService.GetTrades(this.GetClientId())
            };
    }
}
