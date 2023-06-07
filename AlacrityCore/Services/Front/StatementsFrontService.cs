using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Front;
public interface IStatementsFrontService
{
    Task<List<StatementDto>> GetStatements(int clientId);
}

internal class StatementsFrontService : IStatementsFrontService
{
    private readonly IStatementsQuery _query;
    public StatementsFrontService(IStatementsQuery query)
         => (_query) = (query);

    public async Task<List<StatementDto>> GetStatements(int clientId)
        => await _query.GetStatements(clientId);
}
