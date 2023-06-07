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

        Assert.AreEqual(1, positions.PriceHistory.Data.Count);
        Assert.AreEqual(new DateTime(2023, 01, 01), positions.PriceHistory.Data[0].Date);


        var lastPrice = await _query.GetLatestCandle(instrumentId);
        Assert.AreEqual(5, lastPrice.Open);
        Assert.AreEqual(10, lastPrice.High);
        Assert.AreEqual(2, lastPrice.Low);
        Assert.AreEqual(4, lastPrice.Close);
    }
}
