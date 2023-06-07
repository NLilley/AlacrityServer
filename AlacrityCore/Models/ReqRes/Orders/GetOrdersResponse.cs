using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Orders;
public record GetOrdersResponse
{
    public List<OrderDto> Orders { get; set; }
}
