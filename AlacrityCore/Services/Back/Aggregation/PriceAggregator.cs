using AlacrityCore.Enums;
using AlacrityCore.Enums.PriceHistory;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;
using AlacrityCore.Utils;

namespace AlacrityCore.Services.Back.Aggregation;
internal class PriceAggregator : Job<PriceAggregator>
{
    private bool _isInitialized = false;
    public override bool IsInitialized => _isInitialized;
    public override string JobName => nameof(PriceAggregator);

    private static readonly TimeSpan _defaultTimeSpan = TimeSpan.FromSeconds(5);
    private const CandleTimePeriod _defaultPeriod = CandleTimePeriod.Secs5;

    private Dictionary<long, PriceHistoryData> _histories;

    private readonly IPriceHistoryQuery _priceHistoryQuery;
    private readonly IInstrumentsQuery _instrumentsQuery;
    private readonly IExchange _exchange;
    private readonly IMessageNexus _messageNexus;
    public PriceAggregator(
        IALogger logger,
        IPriceHistoryQuery priceHistoryQuery,
        IInstrumentsQuery instrumentsQuery,
        IExchange exchange,
        IMessageNexus messageNexus
    ) : base(logger)
        => (_priceHistoryQuery, _instrumentsQuery, _exchange, _messageNexus) = (priceHistoryQuery, instrumentsQuery, exchange, messageNexus);

    private async Task Initialize()
    {
        if (_isInitialized)
            throw new InvalidOperationException("Cannot initialize PriceAggregator as it is already initialized");

        var instruments = (await _instrumentsQuery.GetInstruments()).Where(i => i.InstrumentId != (long)SpecialInstruments.Cash).ToList();
        _histories = new();

        var random = new Random();
        try
        {
            foreach (var instrument in instruments)
            {
                // For now, only support 5 second data.
                var end = DateTime.UtcNow.Ceil(TimeSpan.FromSeconds(5));
                var start = end.AddSeconds(-5 * PriceHistoryData.CandelLookBack);
                var existingHistory = (await _priceHistoryQuery.GetPriceHistory(instrument.InstrumentId, start, end, _defaultPeriod))
                    .Data.OrderBy(p => p.Date).ToList();

                var latestBest = await _exchange.GetQuote(instrument.InstrumentId);
                if(latestBest.Bid == null || latestBest.Ask == null)
                {
                    // Likely the OrderBook hasn't fully activated yet - Try again shortly
                    await Task.Delay(100);
                    latestBest = await _exchange.GetQuote(instrument.InstrumentId);
                }

                var latestMid = (latestBest.Bid.Value + latestBest.Ask.Value) / 2;

                _logger.Info("PriceHistory missing for instrument {instrumentId} - Mocking price history", instrument.InstrumentId);

                CandleDto lastFoundCandle = null;
                var history = new List<CandleDto>();
                for (var i = 0; i < PriceHistoryData.CandelLookBack; i++)
                {
                    var dateTime = start.AddSeconds(-5 * (i + 1));
                    var foundCandle = existingHistory.FirstOrDefault(c => c.Date == dateTime);
                    if (foundCandle == null)
                    {
                        // Need to predice a sensible price from the previous data.
                        // Note: Current implementation can gap a long way!
                        var close = lastFoundCandle?.Open ?? latestMid;
                        var open = close * (decimal)Math.Pow(1.0001, 10 * (random.NextDouble() - 0.5));
                        var high = close + (0.1M * (close - open) * (decimal)random.NextDouble());
                        var low = close - (0.1M * (close - open) * (decimal)random.NextDouble());
                        if (high < low)
                            (low, high) = (high, low);

                        foundCandle = new()
                        {
                            Date = dateTime,
                            Open = Math.Round(open, 2),
                            High = Math.Round(high, 2),
                            Low = Math.Round(low, 2),
                            Close = Math.Round(close, 2)
                        };

                        await _priceHistoryQuery.AddCandle(instrument.InstrumentId, _defaultPeriod, foundCandle);
                    }

                    history.Add(foundCandle);
                    lastFoundCandle = foundCandle;
                }

                history.Reverse();

                var priceHistoryData = new PriceHistoryData { InstrumentId = instrument.InstrumentId };
                foreach (var h in history)
                    priceHistoryData.Candles.Push(h);

                _histories[instrument.InstrumentId] = priceHistoryData;
            }

            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occured while initializing PriceAggregator");
            return;
        }
    }

    private void CleanUp()
    {
        _isInitialized = false;
        _histories = null;
    }

    protected override void Work()
    {
        var ct = _ct;

        Initialize().Wait();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                ManageAggregations(ct).Wait();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "An error occurred while Managing Aggregations");
            }
        }

        CleanUp();
    }

    private async Task ManageAggregations(CancellationToken ct)
    {
        try
        {
            await Task.Delay(250, ct);
        }
        catch (Exception)
        {
            return;
        }

        var priceHistories = _histories.Values.ToList();
        for (var i = 0; i < priceHistories.Count; i++)
        {
            if (ct.IsCancellationRequested)
                return;

            var history = priceHistories[i];
            try
            {
                var currentBest = await _exchange.GetQuote(history.InstrumentId);
                var currentMid = Math.Round((currentBest.Bid.Value + currentBest.Ask.Value) / 2, 2);

                var currentCandle = history.Candles.Peek();
                var latestCandleTime = DateTime.UtcNow.Ceil(_defaultTimeSpan);

                currentCandle.UpdateClose(currentMid);
                if (latestCandleTime != currentCandle.Date)
                {
                    await _priceHistoryQuery.AddCandle(history.InstrumentId, _defaultPeriod, currentCandle);

                    var newCandle = new CandleDto()
                    {
                        Date = latestCandleTime,
                        Open = currentMid,
                        High = currentMid,
                        Low = currentMid,
                        Close = currentMid
                    };

                    history.Candles.Push(newCandle);
                }

                // Calculate Indicators
                var (rsi, stochastic, proc) = CalculateIndicators(history.Candles);

                var rsiIndicator = new InstrumentIndicatorDto
                {
                    InstrumentId = history.InstrumentId,
                    IndicatorKind = IndicatorKind.Oscillator,
                    Name = "RSI",
                    Value = rsi
                };
                await _instrumentsQuery.UpsertInstrumentIndicator(rsiIndicator);
                _messageNexus.FireMessage(new IndicatorUpdateMessage {
                    Indicator = rsiIndicator
                });

                var stochasticIndicator = new InstrumentIndicatorDto
                {
                    InstrumentId = history.InstrumentId,
                    IndicatorKind = IndicatorKind.Oscillator,
                    Name = "Stochastic",
                    Value = stochastic
                };
                await _instrumentsQuery.UpsertInstrumentIndicator(stochasticIndicator);
                _messageNexus.FireMessage(new IndicatorUpdateMessage
                {
                    Indicator = stochasticIndicator
                });

                var procIndicator = new InstrumentIndicatorDto
                {
                    InstrumentId = history.InstrumentId,
                    IndicatorKind = IndicatorKind.Oscillator,
                    // Price Rate of Change
                    Name = "PRoC",
                    Value = proc
                };
                await _instrumentsQuery.UpsertInstrumentIndicator(procIndicator);
                _messageNexus.FireMessage(new IndicatorUpdateMessage
                {
                    Indicator = procIndicator
                });

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error updating price history for instrument: {instrumentId} ", history.InstrumentId);
            }
        }
    }

    private static (decimal rsi, decimal stochastic, decimal proc) CalculateIndicators(RingBuffer<CandleDto> buffer)
    {
        // Note: Diverging a little from the definitions!
        // https://www.investopedia.com/terms/r/rsi.asp
        // https://www.investopedia.com/terms/s/stochasticoscillator.asp
        // https://www.investopedia.com/terms/p/pricerateofchange.asp
        var numGains = 0;
        var sumGains = 0M;
        var numLosses = 0;
        var sumLosses = 0M;

        decimal? low = null;
        decimal? high = null;

        // Ignore last 
        const int periodsToUse = 14;
        for (var i = buffer.Count - periodsToUse - 1; i < buffer.Count; i++)
        {
            var candle = buffer[i];
            var r = (candle.Close / candle.Open) - 1;
            if (r > 0)
            {
                numGains++;
                sumGains += r;
            }
            else
            {
                numLosses++;
                sumLosses -= r;
            }

            if (low == null || candle.Low < low)
                low = candle.Low;
            if (high == null || candle.High > high)
                high = candle.High;
        }

        var close = buffer.Peek().Close;
        var close14 = buffer[buffer.Count - 14].Close;

        var averageGain = numGains == 0 ? 0 : sumGains / numGains;
        var averageLoss = numLosses == 0 ? 0 : sumLosses / numLosses;

        var rsi = averageLoss == 0
            ? (averageGain == 0 ? 50 : 100)
            : 100 - (100 / (1 + (averageGain / averageLoss)));

        var stochasticDenom = high - low;
        var stochasticIndicator = (decimal)(
            stochasticDenom == 0
                ? 50
                : 100 * ((close - low) / (high - low))
        );

        // Tweak to produce a friendlier number!
        var proc = close14 == 0 ? 0 : ((close - close14) / close14) * 100 * 500;

        return (Math.Round(rsi, 2), Math.Round(stochasticIndicator, 2), Math.Round(proc, 2));
    }

    private record PriceHistoryData
    {
        public const int CandelLookBack = 60;

        public long InstrumentId { get; set; }
        public RingBuffer<CandleDto> Candles { get; set; } = new(CandelLookBack);
    }
}

public class IndicatorUpdateMessage : MessageNexusMessage
{
    public InstrumentIndicatorDto Indicator { get; set; }
}
