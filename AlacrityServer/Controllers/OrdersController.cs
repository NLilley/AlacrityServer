using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes.Orders;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrdersFrontService _ordersFrontService;
    public OrdersController(IOrdersFrontService ordersFrontService)
        => (_ordersFrontService) = (ordersFrontService);

    [HttpGet]
    public async Task<GetOrdersResponse> GetOrders([FromQuery]GetOrdersRequest request)
        => new()
        {
            Orders = await _ordersFrontService.GetOrders(this.GetClientId())
        };

    [HttpPost]
    public async Task<SubmitOrderResponse> SubmitOrder([FromBody] SubmitOrderRequest request)
        => await _ordersFrontService.SubmitOrder(this.GetClientId(), request);

    [HttpDelete]
    public async Task<CancelOrderResponse> CancelOrder([FromBody] CancelOrderRequest request)
        => await _ordersFrontService.CancelOrder(this.GetClientId(), request.OrderId);
}
