namespace AlacrityCore.Models.ReqRes.WebMessages;
public record SubmitMessageRequest
{
    public long? RootMessageId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
}
