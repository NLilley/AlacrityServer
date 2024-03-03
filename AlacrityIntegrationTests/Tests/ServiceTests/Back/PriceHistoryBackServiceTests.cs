using AlacrityCore.Enums.PriceHistory;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Front;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back;
public class PriceHistoryBackServiceTests
{
    private static IPriceHistoryQuery _query;
    private static IPriceHistoryBackService _service;
    private static IPriceHistoryFrontService _frontService;
    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("PriceHistoryBack");
        _query = new PriceHistoryQuery(connection);
        _service = new PriceHistoryBackService(_query);
        _frontService = new PriceHistoryFrontService(_query);
    }

    [Test]
    public async Task AddPositionWorks()
    {
        var instrumentId = 1;

        await _service.AddCandle(instrumentId, CandleTimePeriod.Secs5, new CandleDto
        {
            Date = new DateTime(2023, 01, 01),
            Open = 5,
            High = 10,
            Low = 2,
            Close = 4
        });

        var positions = await _frontService.GetPriceHistory(new()
        {
            InstrumentId = instrumentId,
            Period = CandleTimePeriod.Secs5,
            Start = new DateTime(2022, 01, 01),
            End = new DateTime(2024, 01, 01)
        });

        Assert.That(1, Is.EqualTo(positions.PriceHistory.Data.Count));
        Assert.That(new DateTime(2023, 01, 01), Is.EqualTo(positions.PriceHistory.Data[0].Date));


        var lastPrice = await _query.GetLatestCandle(instrumentId);
        Assert.That(5, Is.EqualTo(lastPrice.Open));
        Assert.That(10, Is.EqualTo(lastPrice.High));
        Assert.That(2, Is.EqualTo(lastPrice.Low));
        Assert.That(4, Is.EqualTo(lastPrice.Close));
    }
}
