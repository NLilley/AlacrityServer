using AlacrityCore.Queries;
using AlacrityCore.Services;
using AlacrityCore.Services.Front;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class StatementsFrontServiceTests
{
    private static StatementsFrontService _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("Statements");
        var statementsQuery = new StatementsQuery(connection);
        _service = new StatementsFrontService(statementsQuery);
    }

    [Test]
    public async Task GetStatements()
    {
        var statements = await _service.GetStatements(1);
    }
}
