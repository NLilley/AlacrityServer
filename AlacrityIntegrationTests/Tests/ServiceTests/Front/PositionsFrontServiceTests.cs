using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Front;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class PositionsFrontServiceTests
{
    private const int _clientId = 1;
    private const long _instrument_id = 1;

    private static PositionsFrontService _service;
    private static PositionsBackService _backService;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("PositionsFront");
        var query = new PositionsQuery(connection);
        _service = new PositionsFrontService(query);
        _backService = new PositionsBackService(query);
    }

    [Test]
    public async Task GetPositions()
    {
        var positions = await _service.GetPositions(_clientId);

        // MaxProfits has 3 positions by default
        Assert.That(3, Is.EqualTo(positions.Count));
    }
}
