using AlacrityCore.Enums;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using Microsoft.Data.Sqlite;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back;
public class LedgerBackServiceTests
{
    public const int _clientId = 3;

    private static SqliteConnection _connection;
    private static LedgerBackService _service;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _connection = await IntegrationFixture.SetUpDatabaseWithPrefix("Ledger-Back");
        var query = new LedgerQuery(_connection);
        _service = new LedgerBackService(query);
    }

    [Test]
    public async Task LedgerIntegration()
    {
        await _service.AddEntry(_clientId, -1, TransactionKind.Deposit, 100);
        await _service.AddEntry(_clientId, -1, TransactionKind.Trade, 200);

        var entries = await _service.GetLedgerEntries(_clientId);

        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual(200, entries[0].Quantity);
        Assert.AreEqual(100, entries[1].Quantity);
    }
}
