using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Positions;
public record GetPositionsResponse
{
    public List<PositionDto> Positions { get; set; }
}
