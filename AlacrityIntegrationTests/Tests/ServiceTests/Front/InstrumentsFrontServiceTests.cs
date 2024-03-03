using AlacrityCore.Enums;
using AlacrityCore.Enums.PriceHistory;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Back.Exchange;
using AlacrityCore.Services.Front;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
internal class InstrumentsFrontServiceTests
{
    private const long _instrumentId = 1;

    private static IPriceHistoryQuery _priceHistoryQuery;
    private static IInstrumentBackService _backService;
    private static IInstrumentFrontService _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var mockLogger = new Mock<IALogger>().Object;
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("InstrumentsFront");
        var ordersQuery = new OrdersQuery(connection);
        var instrumentsQuery = new InstrumentsQuery(connection);
        var tradesQuery = new TradesQuery(connection);
        var positionsQuery = new PositionsQuery(connection);
        var ledgerQuery = new LedgerQuery(connection);
        var messageNexus = new MessageNexus();
        var exchange = new Exchange(mockLogger, ordersQuery, instrumentsQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, new TransactionLock());

        _priceHistoryQuery = new PriceHistoryQuery(connection);
        _backService = new InstrumentBackService(instrumentsQuery);
        _service = new InstrumentFrontService(instrumentsQuery, exchange);
    }

    [Test]
    public async Task GetInstruments()
    {
        var instruments = await _service.GetInstruments();
        Assert.That(instruments.Count > 16, Is.True);
        Assert.That(
            instruments.All(i => i.InstrumentId == (long)SpecialInstruments.Cash || !string.IsNullOrWhiteSpace(i.IconPath)),
            Is.True
        );
    }

    [Test]
    public async Task GetInstrument()
    {
        await _backService.UpsertInstrumentIndicator(new InstrumentIndicatorDto
        {
            InstrumentId = _instrumentId,
            IndicatorKind = IndicatorKind.Oscillator,
            Name = "Test",
            Value = 10
        });

        await _priceHistoryQuery.AddCandle(
            _instrumentId,
            CandleTimePeriod.Secs5,
            new()
            {
                Date = DateTime.UtcNow,
                Close = 500
            }
        );

        var instrument = await _service.GetInstrument(_instrumentId);
        Assert.That(1, Is.EqualTo(instrument.InstrumentId));
        Assert.That("Adobe", Is.EqualTo(instrument.Name));
        Assert.That(500, Is.EqualTo(instrument.PreviousClose));

        var indicators = await _service.GetIndicators(_instrumentId);
        Assert.That(1, Is.EqualTo(indicators.Count));
        Assert.That("Test", Is.EqualTo(indicators["Test"].Name));
    }
}
