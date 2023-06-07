using AlacrityCore.Queries;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class SearchFrontServiceTests
{
    private SearchQuery _searchQuery;
    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix("SearchFront");
        _searchQuery = new SearchQuery(connection);
    }

    [Test]
    public async Task SearchInstruments_NoResults()
    {
        var noResults = await _searchQuery.SearchInstruments("Nothing Should Match");
        Assert.That(noResults.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task SearchInstruments_Microsoft()
    {
        var results = await _searchQuery.SearchInstruments("Microsoft");
        Assert.That(results.Count, Is.EqualTo(1));
        var result = results[0];
        Assert.That(result.Name, Is.EqualTo("Microsoft"));
    }

    [Test]
    public async Task SearchInstruments_ManyMatches()
    {
        var results = await _searchQuery.SearchInstruments("A");
        Assert.That(results.Count, Is.GreaterThan(1));
    }
}
