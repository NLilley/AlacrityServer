using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.WebMessages;
public record GetWebMessageThreadsResponse
{
    public List<WebMessageThreadDto> WebMessageThreads { get; set; }
}
