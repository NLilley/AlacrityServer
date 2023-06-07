using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Front;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back;
public class PositionBackServiceTests
{
    private const int _clientId = 3;
    private const long _instrument_id = 1;

    private static IPositionsBackService _service;
    private static IPositionsFrontService _frontService;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("PositionsBack");
        var query = new PositionsQuery(connection);
        _service = new PositionsBackService(query);
        _frontService = new PositionsFrontService(query);
    }

    [Test]
    public async Task AddPosition()
    {        
        var (pos1, price1) = await _service.AddToPosition(_clientId, _instrument_id, 10, 100);
        var (pos2, price2) = await _service.AddToPosition(_clientId, _instrument_id, 20, 200);

        Assert.AreEqual(10, pos1);
        Assert.AreEqual(100, price1);
        Assert.AreEqual(30, pos2);
        Assert.AreEqual(166.67, Math.Round(price2, 2));

        var positions = await _frontService.GetPositions(_clientId);
        var position = positions.Find(p => p.InstrumentId == _instrument_id);
        Assert.That(position.Quantity, Is.EqualTo(30));
        Assert.That(position.AveragePrice, Is.InRange(166, 167));

        await _service.AddToPosition(_clientId, _instrument_id, -5, 50);
        await _service.AddToPosition(_clientId, _instrument_id, -15, 150);
        await _service.AddToPosition(_clientId, _instrument_id, -20, 200);

        var positions2 = await _frontService.GetPositions(_clientId);
        var position2 = positions2.Find(i => i.InstrumentId == _instrument_id);
        Assert.That(position2.Quantity, Is.EqualTo(-10));

        var (lastPos, lastPrice) = await _service.AddToPosition(_clientId, _instrument_id, 10, 100);
        var positions3 = await _frontService.GetPositions(_clientId);

        Assert.AreEqual(0M, lastPos);
        // Position gets deleted once hitting 0
        Assert.That(positions3, Is.Empty);
    }
}
