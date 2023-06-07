using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Models.Back;

namespace AlacrityCore.Services.Back.Exchange;

internal interface IMarketParticipant
{
    public int Id { get; }
    public Task Act(List<(long instrumentId, decimal? price)> latestInstrumentPrices);
}

internal class MarketParticipant : IMarketParticipant
{
    public int Id { get; private set; }

    private DateTime _lastAction = DateTime.UtcNow;
    private decimal _variability = 0;
    private readonly Dictionary<long, InstrumentValuation> InstrumentValuations = new();
    private readonly Random _rng = new();

    private readonly IExchange _exchange;
    private readonly IPriceHistoryQuery _priceHistoryQuery;
    public MarketParticipant(int id, IExchange exchange, IPriceHistoryQuery priceHistoryQuery)
        => (Id, _exchange, _priceHistoryQuery) = (id, exchange, priceHistoryQuery);

    public async Task Initialize(List<(long instrumentId, decimal? price)> lastestInstrumentPrices, decimal variability)
    {
        _variability = variability;
        await Act(lastestInstrumentPrices);
    }

    public async Task Act(List<(long instrumentId, decimal? price)> lastestInstrumentPrices)
    {
        var now = DateTime.UtcNow;

        foreach (var instrumentPrice in lastestInstrumentPrices)
        {
            var currentValuation = InstrumentValuations.GetValueOrDefault(instrumentPrice.instrumentId);

            var (bid, ask) = GetLatestValues(
                instrumentPrice.price,
                currentValuation?.Bid ?? instrumentPrice.price,
                currentValuation?.Ask ?? instrumentPrice.price,
                (decimal)(now - _lastAction).TotalSeconds,
                instrumentPrice.instrumentId
            );

            var bidQuantity = Math.Floor((decimal)((_rng.NextDouble() * 0.9) + 0.1) * 1000);
            var askQuantity = Math.Floor((decimal)((_rng.NextDouble() * 0.9) + 0.1) * 1000);

            var latestBid = await _exchange.GetOrder(instrumentPrice.instrumentId, currentValuation?.CurrentBid.OrderId ?? 0);
            var latestAsk = await _exchange.GetOrder(instrumentPrice.instrumentId, currentValuation?.CurrentAsk.OrderId ?? 0);

            if (currentValuation?.CurrentBid != null && latestBid?.OrderStatus == OrderStatus.Active)
                await _exchange.CancelOrder(latestBid.InstrumentId, latestBid.OrderId);
            if (currentValuation?.CurrentAsk != null && latestAsk?.OrderStatus == OrderStatus.Active)
                await _exchange.CancelOrder(latestAsk.InstrumentId, latestAsk.OrderId);

            var bidOrder = await _exchange.SubmitOrder(new Order
            {
                InstrumentId = instrumentPrice.instrumentId,
                OrderDirection = TradeDirection.Buy,
                LimitPrice = bid,
                OrderKind = OrderKind.LimitOrder,
                Quantity = bidQuantity
            });

            var askOrder = await _exchange.SubmitOrder(new Order
            {
                InstrumentId = instrumentPrice.instrumentId,
                OrderDirection = TradeDirection.Sell,
                LimitPrice = ask,
                OrderKind = OrderKind.LimitOrder,
                Quantity = askQuantity
            });

            InstrumentValuations[instrumentPrice.instrumentId] = new()
            {
                InstrumentId = instrumentPrice.instrumentId,
                Bid = bid,
                Ask = ask,
                CurrentAsk = askOrder,
                CurrentBid = bidOrder
            };
        }

        _lastAction = now;
    }


    private (decimal bid, decimal ask) GetLatestValues(decimal? latestPrice, decimal? ourBid, decimal? ourAsk, decimal secondsSinceLastValuation, long instrumentId)
    {
        if (latestPrice == null || ourBid == null || ourAsk == null)
        {
            var latestCandle = _priceHistoryQuery.GetLatestCandle(instrumentId).Result;
            if (latestCandle != null)
            {
                latestPrice = latestCandle.Close;
                ourBid = (decimal)0.9 * latestCandle.Close;
                ourAsk = (decimal)(1 / 0.9) * latestCandle.Close;
            }
            else
            {
                // For whatever reason there are no prices in the exchange.
                // Generate an arbitrary price.  
                latestPrice = (decimal)(_rng.NextDouble() * 4000) + 100;
                ourBid = latestPrice - 1;
                ourAsk = latestPrice + 1;
            }
        }

        var ourMid = (ourBid + ourAsk) / 2;

        var delta = Math.Min(1, secondsSinceLastValuation);
        var amountToConverge = delta * 0.2M;

        var newMid = ourMid + ((latestPrice - ourMid) * amountToConverge);

        var midVariation = (decimal)Math.Pow(1.05, (_rng.NextDouble() - 0.5) * (double)_variability);
        newMid *= midVariation;

        var spreadVariation = _rng.NextDouble() * (double)_variability;
        var bidDecay = Math.Pow(0.9, spreadVariation);
        var bid = Math.Round(newMid.Value * (decimal)bidDecay, 2);
        var askGrowth = Math.Pow(0.9, -spreadVariation);
        var ask = Math.Round(newMid.Value * (decimal)askGrowth, 2);

        return (bid, ask);
    }

    private class InstrumentValuation
    {
        public long InstrumentId { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public Order CurrentBid { get; set; }
        public Order CurrentAsk { get; set; }
    }
}
