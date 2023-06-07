namespace AlacrityCore.Models.ReqRes.Authenatication;
public record LoginRequest
{
    public string UserName { get; set; }
    public string Password { get; set; }
}
