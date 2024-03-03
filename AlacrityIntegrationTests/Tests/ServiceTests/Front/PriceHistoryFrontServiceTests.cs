using AlacrityCore.Enums.PriceHistory;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Front;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class PriceHistoryFrontServiceTests
{
    private static IPriceHistoryBackService _backService;
    private static IPriceHistoryFrontService _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("PriceHistoryFronts");
        var query = new PriceHistoryQuery(connection);
        _backService = new PriceHistoryBackService(query);
        _service = new PriceHistoryFrontService(query);
    }

    [Test]
    public async Task GetPositionsWorks()
    {
        var instrumentId = 1;
        await _backService.AddCandle(instrumentId, CandleTimePeriod.Secs5, new CandleDto
        {
            Date = new DateTime(2023, 01, 01),
            Open = 1,
            High = 1,
            Low = 1,
            Close = 1
        });

        await _backService.AddCandle(instrumentId, CandleTimePeriod.Secs5, new CandleDto
        {
            Date = new DateTime(2024, 01, 01),
            Open = 2,
            High = 2,
            Low = 2,
            Close = 2
        });

        await _backService.AddCandle(instrumentId, CandleTimePeriod.Secs5, new CandleDto
        {
            Date = new DateTime(2025, 01, 01),
            Open = 3,
            High = 3,
            Low = 3,
            Close = 3
        });

        await _backService.AddCandle(instrumentId, CandleTimePeriod.Secs5, new CandleDto
        {
            Date = new DateTime(2026, 01, 01),
            Open = 4,
            High = 4,
            Low = 4,
            Close = 4
        });

        var history = await _service.GetPriceHistory(new()
        {
            InstrumentId = instrumentId,
            Period = CandleTimePeriod.Secs5,
            Start = new DateTime(2024, 01, 01),
            End = new DateTime(2026, 01, 01),
        });

        // Should skip first record, and drop last record
        Assert.That(2, Is.EqualTo(history.PriceHistory.Data.Count));
        Assert.That(2, Is.EqualTo(history.PriceHistory.Data[0].Open));
        Assert.That(3, Is.EqualTo(history.PriceHistory.Data[1].Open));
    }
}
