using AlacrityCore.Enums;
using AlacrityCore.Infrastructure;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Aggregation;
using AlacrityCore.Services.Back.Exchange;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back.ExchangeTests;
internal class PriceAggregatorTests
{
    private async Task<Dependencies>
    GetDependencies()
    {
        var mockExchangeLogger = new Mock<IALogger>().Object;
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix($"PriceAggregator-{Guid.NewGuid().ToString()[0..8]}");

        var ordersQuery = new OrdersQuery(connection);
        var instrumentsQuery = new InstrumentsQuery(connection);
        var tradesQuery = new TradesQuery(connection);
        var positionsQuery = new PositionsQuery(connection);
        var priceHistoryQuery = new PriceHistoryQuery(connection);
        var ledgerQuery = new LedgerQuery(connection);
        var messageNexus = new MessageNexus();

        var exchange = new Exchange(mockExchangeLogger, ordersQuery, instrumentsQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, new TransactionLock());
        var marketParticipant = new MarketParticipant(1, exchange, priceHistoryQuery);
        var priceAggregator = new PriceAggregator(mockExchangeLogger, priceHistoryQuery, instrumentsQuery, exchange, messageNexus);

        return new Dependencies
        {
            OrdersQuery = ordersQuery,
            TradesQuery = tradesQuery,
            InstrumentsQuery = instrumentsQuery,
            Exchange = exchange,
            MarketParticipant = marketParticipant,
            PriceAggregator = priceAggregator
        };
    }

    [Ignore("Test needs to be manually run")]
    [Test]
    public async Task BehavesCorrectly()
    {
        var dependencies = await GetDependencies();
        var instrumentQuery = dependencies.InstrumentsQuery;
        var exchange = dependencies.Exchange;
        var marketParticipant = dependencies.MarketParticipant;
        var priceAggregator = dependencies.PriceAggregator;

        var random = new Random();
        var instrumentPrices = (await instrumentQuery.GetInstruments()).Where(i => i.InstrumentId != (long)SpecialInstruments.Cash)
            .Select(i => (i.InstrumentId, (decimal?)random.NextDouble() * 5000))
            .ToList();

        // PriceAggregator initialization requires Excahnge initialization to have completed properly
        exchange.Start();
        Thread.Sleep(200);

        await marketParticipant.Initialize(instrumentPrices, 10M);
        Thread.Sleep(200);

        priceAggregator.Start();
        Thread.Sleep(100_000);

        priceAggregator.Stop();
    }

    private class Dependencies
    {
        public OrdersQuery OrdersQuery { get; set; }
        public TradesQuery TradesQuery { get; set; }
        public InstrumentsQuery InstrumentsQuery { get; set; }
        public Exchange Exchange { get; set; }
        public MarketParticipant MarketParticipant { get; set; }
        public PriceAggregator PriceAggregator { get; set; }
    }
}
