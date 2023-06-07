namespace AlacrityCore.Models.ReqRes.Orders;
public record SubmitOrderResponse
{
    public bool Succeeded { get; set; }
    public long? OrderId { get; set; }
    public string FailureReason { get; set; }
}
