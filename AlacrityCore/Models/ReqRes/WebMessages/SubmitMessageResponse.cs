namespace AlacrityCore.Models.ReqRes.WebMessages;
public record SubmitMessageResponse
{
    public bool Succeeded { get; set; }
    public string ErrorMessage { get; set; }
}
