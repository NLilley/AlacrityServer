using AlacrityCore.Queries;
using AlacrityCore.Services.Front;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;

[TestFixture]
public class WatchlistFrontServiceTests
{
    private const int _clientId = 1;
    private const long _instrumentId = 1;

    private static WatchlistsFrontService _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("Watchlist");
        var query = new WatchlistQuery(connection);
        _service = new WatchlistsFrontService(new Mock<ILogger<WatchlistsFrontService>>().Object, query);
    }

    [Test]
    public async Task GetWatchlists_Empty()
    {
        var watchlists = await _service.GetWatchlists(-1);
        Assert.That(watchlists.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetWatchlists_Full()
    {
        var watchlistId1 = await _service.AddWatchlist(1, "Another Test Watchlist");
        var watchlistItemId1 = await _service.AddToWatchlist(_clientId, watchlistId1.Value, _instrumentId, 1);

        var watchlistId2 = await _service.AddWatchlist(_clientId, "And Another Test Watchlist");

        var newWatchlists = await _service.GetWatchlists(1);
        newWatchlists.Reverse();

        Assert.That(newWatchlists[1].WatchlistItems.Count, Is.EqualTo(1));
        Assert.That(newWatchlists[0].WatchlistItems.Count, Is.EqualTo(0));

        Assert.That(newWatchlists[1].Name, Is.EqualTo("Another Test Watchlist"));
        Assert.That(newWatchlists[0].Name, Is.EqualTo("And Another Test Watchlist"));

        Assert.That(newWatchlists[1].WatchlistItems[0].InstrumentId, Is.EqualTo(1));
    }

    [Test]
    public async Task AddWatchlist()
    {
        var watchlistId = await _service.AddWatchlist(_clientId, "Test Watchlist");
        Assert.That(watchlistId > 0, Is.True);
    }

    [Test]
    public async Task AddToWatchlist()
    {
        var watchlistId = await _service.AddWatchlist(_clientId, "Another Test Watchlist!");
        var watchlistItemId = await _service.AddToWatchlist(_clientId, watchlistId.Value, _instrumentId, 1);
        Assert.That(watchlistItemId > 0, Is.True);
    }

    [Test]
    public async Task UpdateWatchlistName()
    {
        var newName = "Definitely A Watchlist";
        var watchlistId = await _service.AddWatchlist(_clientId, "Definitely NOT A Watchlist");
        await _service.UpdateWatchlistName(_clientId, watchlistId.Value, newName);

        var watchlists = await _service.GetWatchlists(_clientId);
        Assert.That(watchlists.Find(w => w.WatchlistId == watchlistId).Name, Is.EqualTo(newName));
    }

    [Test]
    public async Task DeleteWatchlist()
    {
        var name = "Bad Watchlist";
        var watchlistId = await _service.AddWatchlist(_clientId, name);
        await _service.DeleteWatchlist(_clientId, watchlistId.Value);
        var watchlists = await _service.GetWatchlists(_clientId);
        Assert.That(watchlists.FirstOrDefault(w => w.Name == name), Is.Null);
    }

    [Test]
    public async Task DeleteWatchlistItem()
    {
        var watchlistId = await _service.AddWatchlist(_clientId, "Bad Children");
        var watchlistItemId = await _service.AddToWatchlist(_clientId, watchlistId.Value, _instrumentId, 1);
        await _service.DeleteWatchlistItem(1, watchlistItemId.Value);

        var watchlist = (await _service.GetWatchlists(_clientId)).Find(w => w.WatchlistId == watchlistId);
        Assert.That(watchlist.WatchlistItems.Count == 0, Is.True);
    }
}
