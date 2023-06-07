using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes.Statements;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AlacrityServer.Controllers;

[ApiController]
[Route("statements")]
public class StatementsController : ControllerBase
{
    private readonly IStatementsFrontService _statementsFrontService;
    public StatementsController(IStatementsFrontService statementsFrontService)
        => (_statementsFrontService) = (statementsFrontService);

    [HttpGet]
    public async Task<GetStatementResponse> GetStatements([FromQuery]GetStatementRequest reqeust)
        => new()
        {
            Statements = await _statementsFrontService.GetStatements(this.GetClientId())
        };
}
