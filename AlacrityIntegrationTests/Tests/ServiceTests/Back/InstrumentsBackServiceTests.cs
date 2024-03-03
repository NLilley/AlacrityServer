using AlacrityCore.Infrastructure;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Back.Exchange;
using AlacrityCore.Services.Front;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back;
internal class InstrumentsBackServiceTests
{
    private const long _instrument_id = 1;

    private static IInstrumentBackService _service;
    private static IInstrumentFrontService _frontService;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var mockLogger = new Mock<IALogger>().Object;
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("InstrumentsBack");
        var ordersQuery = new OrdersQuery(connection);
        var instrumentsQuery = new InstrumentsQuery(connection);
        var tradesQuery = new TradesQuery(connection);
        var positionsQuery = new PositionsQuery(connection);
        var ledgerQuery = new LedgerQuery(connection);
        var messageNexus = new MessageNexus();
        var exchange = new Exchange(mockLogger, ordersQuery, instrumentsQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, new TransactionLock());

        _service = new InstrumentBackService(instrumentsQuery);
        _frontService = new InstrumentFrontService(instrumentsQuery, exchange);
    }

    [Test]
    public async Task UpsertInstrumentIndicator()
    {
        await _service.UpsertInstrumentIndicator(new InstrumentIndicatorDto
        {
            InstrumentId = _instrument_id,
            IndicatorKind = AlacrityCore.Enums.IndicatorKind.Oscillator,
            Name = "Test",
            Value = 10
        });

        var indicators = await _frontService.GetIndicators(_instrument_id);
        Assert.That(1, Is.EqualTo(indicators.Count));
        Assert.That("Test", Is.EqualTo(indicators["Test"].Name));
        Assert.That(10, Is.EqualTo(indicators["Test"].Value));

        await _service.UpsertInstrumentIndicator(new InstrumentIndicatorDto
        {
            InstrumentId = _instrument_id,
            IndicatorKind = AlacrityCore.Enums.IndicatorKind.Oscillator,
            Name = "Test",
            Value = 50
        });

        var indiactors2 = await _frontService.GetIndicators(_instrument_id);
        Assert.That(1, Is.EqualTo(indiactors2.Count));
        Assert.That("Test", Is.EqualTo(indiactors2["Test"].Name));
        Assert.That(50, Is.EqualTo(indiactors2["Test"].Value));
    }
}
