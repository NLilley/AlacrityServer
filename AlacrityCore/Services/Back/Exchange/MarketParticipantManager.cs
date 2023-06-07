using AlacrityCore.Enums;
using AlacrityCore.Enums.PriceHistory;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;

namespace AlacrityCore.Services.Back.Exchange;
internal class MarketParticipantManager : Job<MarketParticipantManager>
{
    private bool _isInitialized = false;
    public override bool IsInitialized => _isInitialized;
    public override string JobName => nameof(MarketParticipantManager);

    private List<InstrumentBriefDto> _instruments;
    private List<IMarketParticipant> _marketParticipants = new();

    private readonly IInstrumentsQuery _instrumentsQuery;
    private readonly IPriceHistoryQuery _priceHistoryQuery;
    private readonly IExchange _exchange;
    internal MarketParticipantManager(
        IALogger logger,
        IInstrumentsQuery instrumentsQuery,
        IPriceHistoryQuery priceHistoryQuery,
        IExchange exchange
    ) : base(logger)
        => (_instrumentsQuery, _priceHistoryQuery, _exchange) = (instrumentsQuery, priceHistoryQuery, exchange);

    public void CleanUp()
    {
        _isInitialized = false;
        _marketParticipants = null;
        _instruments = null;
    }

    protected override void Work()
    {
        var ct = _ct;

        Initialize().Wait();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                ManageParticipants(ct).Wait();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error thrown while managing participants");
            }
        }

        CleanUp();
    }

    private async Task Initialize()
    {
        if (_isInitialized)
            throw new InvalidCastException("Cannot initialize MarketParticipantManager as it is already intialized");

        _logger.Info("Initializing MarketParticipantManager");

        _instruments = (await _instrumentsQuery.GetInstruments())
            .Where(i => i.InstrumentId != (long)SpecialInstruments.Cash)
            .ToList();

        var initialPrices = await GetLastPricesFromDB();

        _marketParticipants = new();
        for (var i = 0; i < 5; i++)
        {
            var participant = new MarketParticipant(i, _exchange, _priceHistoryQuery);
            await participant.Initialize(initialPrices, 0.004M +  (0.00005M * (i + 1)));
            _marketParticipants.Add(participant);
        }               

        _isInitialized = true;
    }

    private int _manageCount = 0;
    private async Task ManageParticipants(CancellationToken ct)
    {
        try
        {
            await Task.Delay(250, ct);
        }
        catch (Exception)
        {
            return;
        }

        var lastPrices = await GetLastPriceFromExchange();
        foreach (var marketParticipant in _marketParticipants)
        {
            // Don't manage every participant on every loop
            if (_manageCount % _marketParticipants.Count == marketParticipant.Id)
                await marketParticipant.Act(lastPrices);
        }

        _manageCount++;
    }

    private async Task<List<(long instrumentId, decimal? price)>> GetLastPricesFromDB()
    {
        var random = new Random();
        var initialPrices = new List<(long instrumentId, decimal? intialPrice)>();
        foreach (var instrument in _instruments)
        {
            var lastPrice = (await _priceHistoryQuery.GetPriceHistory(instrument.InstrumentId, DateTime.MinValue, DateTime.MaxValue, CandleTimePeriod.Secs5))
                .Data
                .OrderByDescending(p => p.Date)
                .FirstOrDefault();

            decimal price;
            if (lastPrice == null)
                price = 50 + (decimal)(random.NextDouble() * 5000);
            else
            {
                // Allow up to a 20% swing in price
                var timeSinceLastPrice = (DateTime.UtcNow - lastPrice.Date).TotalDays;
                price = ((lastPrice.Open + lastPrice.Close) / 2)
                    * (decimal)Math.Pow(1 + ((random.NextDouble() - 0.5) * 0.2), timeSinceLastPrice);
            }

            initialPrices.Add((instrument.InstrumentId, price));
        }

        return initialPrices;
    }

    private async Task<List<(long instrumentId, decimal? price)>> GetLastPriceFromExchange()
    {
        var lastPrices = new List<(long instrumentId, decimal? lastPrice)>();
        foreach (var instrument in _instruments)
        {
            var quote = await _exchange.GetQuote(instrument.InstrumentId);
            lastPrices.Add((instrument.InstrumentId, (decimal?)(quote.Bid + quote.Ask) / 2));
        }

        return lastPrices;
    }
}
