using AlacrityCore.Models.DTOs;

namespace AlacrityCore.Models.ReqRes.Statements;
public record GetStatementResponse
{
    public List<StatementDto> Statements { get; set; }
}
