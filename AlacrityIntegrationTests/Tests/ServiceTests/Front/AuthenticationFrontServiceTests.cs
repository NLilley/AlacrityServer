using AlacrityCore.Queries;
using AlacrityCore.Services.Front;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class AuthenticationFrontServiceTests
{
    private static AuthenticationFrontService _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("Authentication");
        var authenticationQuery = new AuthenticationQuery(connection);
        _service = new AuthenticationFrontService(authenticationQuery);
    }

    [Test]
    public async Task LoginWithCorrectPassword()
    {
        var result = await _service.Login("MaXpRoFiTs", "ToTheMoon+1");

        Assert.That(result.clientId, Is.EqualTo(1));
        Assert.That(result.error, Is.EqualTo(null));
    }
}
