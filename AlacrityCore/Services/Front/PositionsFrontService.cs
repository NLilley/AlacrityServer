using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Front;
public interface IPositionsFrontService
{
    Task<List<PositionDto>> GetPositions(int clientId);
}

internal class PositionsFrontService : IPositionsFrontService
{
    private readonly IPositionsQuery _query;
    public PositionsFrontService(IPositionsQuery query)
        => (_query) = (query);

    public async Task<List<PositionDto>> GetPositions(int clientId)
        => await _query.GetPositions(clientId);   
}
