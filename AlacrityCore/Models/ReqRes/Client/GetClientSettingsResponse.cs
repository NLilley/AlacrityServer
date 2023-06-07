using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Client;
public record GetClientSettingsResponse
{
    public ClientSettingsDto ClientSettings { get; set; }
}
