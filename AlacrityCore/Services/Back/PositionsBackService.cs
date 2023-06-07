using AlacrityCore.Queries;

namespace AlacrityCore.Services.Back;

internal interface IPositionsBackService
{
    Task<(decimal quantity, decimal averagePrice)> AddToPosition(int clientId, long instrumentId, decimal quantity, decimal price);
}

internal class PositionsBackService : IPositionsBackService
{
    private readonly IPositionsQuery _query;
    public PositionsBackService(IPositionsQuery query)
        => (_query) = (query);

    public async Task<(decimal quantity, decimal averagePrice)> AddToPosition(int clientId, long instrumentId, decimal quantity, decimal price)
        => await _query.AddToPosition(clientId, instrumentId, quantity, price);
}
