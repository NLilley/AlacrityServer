using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Client;
public record GetClientResponse
{
    public ClientDto Client { get; set; }
}
