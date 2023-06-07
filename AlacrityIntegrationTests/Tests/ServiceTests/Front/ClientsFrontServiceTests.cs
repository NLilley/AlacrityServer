using AlacrityCore.Enums.Client;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.ReqRes;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Front;
using AlacrityCore.Utils;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class ClientsFrontServiceTests
{
    private static SqliteConnection _connection;
    private static ClientsFrontService _service;
    private static ClientsBackService _backService;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _connection = await IntegrationFixture.SetUpDatabaseWithPrefix("Clients");
        var logger = new Mock<IALogger>().Object;
        var query = new ClientsQuery(_connection);
        var webMessageUserQuery = new WebMessageUserQuery(_connection);
        _service = new ClientsFrontService(query);
        _backService = new ClientsBackService(logger, query, webMessageUserQuery);
    }

    [Test]
    public async Task ClientServiceIntegration()
    {
        var newClientId = await _backService.AddClient(new NewClientRequest
        {
            UserName = "slickt",
            Password = "TightSecurity",
            FirstName = "Slick",
            OtherNames = "Trader",
            Email = "slick.trader@test.com",
        });

        var client = await _service.GetClient(newClientId);

        Assert.That(client.FirstName, Is.EqualTo("Slick"));
        Assert.That(client.OtherNames, Is.EqualTo("Trader"));

        var hashedPassword = _connection.ExecuteScalar<string>(
            "SELECT hashed_password FROM clients WHERE client_id = @ClientId",
            new { ClientId = newClientId }
        );

        var loginRequest = AuthenticationUtil.VerifyPassword("TightSecurity", hashedPassword);
        Assert.That(loginRequest, Is.EqualTo(PasswordVerificationResult.Success));

        var settings = await _service.GetClientSettings(newClientId);
        Assert.That(settings.IsTelemetryEnabled, Is.EqualTo(false));
        Assert.That(settings.SessionDurationMins, Is.EqualTo(24 * 60));
        Assert.That(settings.VisualizationQuality, Is.EqualTo(VisualizationQuality.High));

        await _service.SetClientSetting(newClientId, "telemetry_preference", "enabled");
        var updatedSettings = await _service.GetClientSettings(newClientId);
        Assert.That(updatedSettings.IsTelemetryEnabled, Is.EqualTo(true));
        Assert.That(updatedSettings.SessionDurationMins, Is.EqualTo(24 * 60));
        Assert.That(updatedSettings.VisualizationQuality, Is.EqualTo(VisualizationQuality.High));
    }
}
