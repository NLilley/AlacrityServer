namespace AlacrityCore.Models.ReqRes.WebMessages;
public record SetMessageFinalizedRequest
{
    public long RootMessageId { get; set; }
    public bool IsFinalized { get; set; }
}
