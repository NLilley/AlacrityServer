using AlacrityCore.Models.Back;
using AlacrityCore.Queries;

namespace AlacrityIntegrationTests.Tests.QueriesTests;

public class TradesQueryTests
{
    private static ITradesQuery _tradesQuery;

    private readonly Trade _dummyTrade = new()
    {
        ClientId = 1,
        InstrumentId = 1,
        OrderId = 1,
        Price = 100,
        Quantity = 100,
        TradeDate = DateTime.UtcNow,
    };

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("TradesQuery");
        _tradesQuery = new TradesQuery(connection);
    }

    public async Task GetTrades()
    {
        var trades = await _tradesQuery.GetTrades(1);
        Assert.That(1, Is.EqualTo(trades.Count));

        var trade = trades[0];
        Assert.That(1, Is.EqualTo(trade.TradeId));
        Assert.That(1, Is.EqualTo(trade.ClientId));
        Assert.That(1, Is.EqualTo(trade.InstrumentId));
        Assert.That(1, Is.EqualTo(trade.OrderId));
        Assert.That(1000, Is.EqualTo(trade.Quantity));
        Assert.That(100, Is.EqualTo(trade.Price));
    }

    public async Task AddTrades()
    {
        await _tradesQuery.AddTrade(_dummyTrade);

        var trades = await _tradesQuery.GetTrades(1);
        Assert.That(2, Is.EqualTo(trades.Count));

        var trade = trades[1];
        Assert.That(3, Is.EqualTo(trade.TradeId));
        Assert.That(1, Is.EqualTo(trade.ClientId));
        Assert.That(1, Is.EqualTo(trade.InstrumentId));
        Assert.That(1, Is.EqualTo(trade.OrderId));
        Assert.That(100, Is.EqualTo(trade.Quantity));
        Assert.That(100, Is.EqualTo(trade.Price));
    }
}
