namespace AlacrityCore.Models.ReqRes.WebMessages;
public record SetMessageReadRequest
{
    public long RootMessageId { get; set; }
    public bool IsRead { get; set; }
}
