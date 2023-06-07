namespace AlacrityCore.Models.ReqRes.Authenatication;
public record ChangePasswordResponse
{
    public bool Succeeded { get; set; }
    public string ErrorMessage { get; set; }
}
