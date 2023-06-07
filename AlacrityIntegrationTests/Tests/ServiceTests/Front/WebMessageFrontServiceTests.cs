using AlacrityCore.Queries;
using AlacrityCore.Services.Front;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class WebMessageFrontServiceTests
{
    private static WebMessagesFrontService _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("WebMessages");
        var webMessageQuery = new WebMessageQuery(connection);
        _service = new WebMessagesFrontService(new Mock<ILogger<WebMessagesFrontService>>().Object, webMessageQuery);
    }

    [Test]
    public async Task GetWebMessageThreads()
    {
        var threads = await _service.GetWebMessageThreads(1);
    }

    [Test]
    public async Task MarkMessageAsRead()
    {
        await _service.SetMessageRead(1, 1, true);
    }

    [Test]
    public async Task MarkMessageAsFinalized()
    {
        await _service.SetMessageFinalized(1, 1, true);
    }
}