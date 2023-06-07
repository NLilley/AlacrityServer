using AlacrityCore.Infrastructure;
using AlacrityCore.Models.ReqRes;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Front;
using Microsoft.Data.Sqlite;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back;
internal class ClientsBackServiceTests
{
    private static SqliteConnection _connection;
    private static ClientsBackService _service;
    private static ClientsFrontService _frontService;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _connection = await IntegrationFixture.SetUpDatabaseWithPrefix("Clients-Back");
        var logger = new Mock<IALogger>().Object;
        var query = new ClientsQuery(_connection);
        var webMessageUserQuery = new WebMessageUserQuery(_connection);
        _service = new ClientsBackService(logger, query, webMessageUserQuery);
        _frontService = new ClientsFrontService(query);
    }

    [Test]
    public async Task AddClientBehavesWell()
    {
        var clientId = await _service.AddClient(new NewClientRequest
        {
            UserName = "slickt",
            Password = "TightSecurity",
            FirstName = "Slick",
            OtherNames = "Trader",
            Email = "slick.trader@test.com",
        });

        var client = await _frontService.GetClient(clientId);
        Assert.That(client.FirstName, Is.EqualTo("Slick"));
    }
}
