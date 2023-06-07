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
        Assert.AreEqual(1, trades.Count);

        var trade = trades[0];
        Assert.AreEqual(1, trade.TradeId);
        Assert.AreEqual(1, trade.ClientId);
        Assert.AreEqual(1, trade.InstrumentId);
        Assert.AreEqual(1, trade.OrderId);
        Assert.AreEqual(1000, trade.Quantity);
        Assert.AreEqual(100, trade.Price);
    }

    public async Task AddTrades()
    {
        await _tradesQuery.AddTrade(_dummyTrade);

        var trades = await _tradesQuery.GetTrades(1);
        Assert.AreEqual(2, trades.Count);

        var trade = trades[1];
        Assert.AreEqual(3, trade.TradeId);
        Assert.AreEqual(1, trade.ClientId);
        Assert.AreEqual(1, trade.InstrumentId);
        Assert.AreEqual(1, trade.OrderId);
        Assert.AreEqual(100, trade.Quantity);
        Assert.AreEqual(100, trade.Price);
    }
}
