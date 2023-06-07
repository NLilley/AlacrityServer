using AlacrityCore.Queries;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class TradesFrontServiceTests
{
    private static TradesQuery _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("Trades");
        _service = new TradesQuery(connection);
    }

    [Test]
    public async Task GetTrades()
    {
        var trades = await _service.GetTrades(1);
    }
}
