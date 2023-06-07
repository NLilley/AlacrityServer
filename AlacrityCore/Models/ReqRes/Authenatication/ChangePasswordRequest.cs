namespace AlacrityCore.Models.ReqRes.Authenatication;
public record ChangePasswordRequest
{
    public string ExistingPassword { get; set; }
    public string NewPassword { get; set; }
}
