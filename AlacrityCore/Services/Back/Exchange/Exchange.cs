using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.Back;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using System.Collections.Concurrent;

namespace AlacrityCore.Services.Back.Exchange;

internal interface IExchange
{
    public Task<QuoteDto> GetQuote(long instrumentId);
    public Task<Order> GetOrder(long instrumentId, long orderId);
    public Task<Order> SubmitOrder(Order newOrder);
    Task CancelOrder(long instrumentId, long orderId);
}

/// <summary>
/// Note: It's assumed that this class will be accessed from a SingleThreaded/ThreadSafe way.
/// public methods are not ThreadSafe!
/// </summary>
internal class Exchange : Job<Exchange>, IExchange
{
    private bool _isInitialized = false;
    public override bool IsInitialized => _isInitialized;
    public override string JobName => nameof(Exchange);

    private long _lastOfferId;
    private Dictionary<long, OrderBook> _orderBooks;
    private ConcurrentQueue<OrderBookOperation> _operations;

    private readonly IOrdersQuery _ordersQuery;
    private readonly IInstrumentsQuery _instrumentsQuery;
    private readonly ITradesQuery _tradesQuery;
    private readonly IPositionsQuery _positionsQuery;
    private readonly ILedgerQuery _ledgerQuery;
    private readonly IMessageNexus _messageNexus;
    private readonly ITransactionLock _transactionLock;

    public Exchange(
        IALogger logger,
        IOrdersQuery ordersQuery,
        IInstrumentsQuery instrumentsQuery,
        ITradesQuery tradesQuery,
        IPositionsQuery positionsQuery,
        ILedgerQuery ledgerQuery,
        IMessageNexus messageNexus,
        ITransactionLock transactionLock
    ) : base(logger)
        => (_ordersQuery, _instrumentsQuery, _tradesQuery, _positionsQuery, _ledgerQuery, _messageNexus, _transactionLock)
        = (ordersQuery, instrumentsQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, transactionLock);


    public async Task<QuoteDto> GetQuote(long instrumentId)
    {
        if (!_isInitialized)
            throw new ArgumentException($"Cannot get quote for instrument {instrumentId} as Exchange has not been initialized");

        if (!_orderBooks.TryGetValue(instrumentId, out var orderBook))
            throw new ArgumentException($"Cannot get quote for instrument {instrumentId} as it is unknown");

        return new()
        {
            InstrumentId = instrumentId,
            Bid = orderBook.BestBid,
            BidSize = orderBook.BestBidSize,
            Ask = orderBook.BestAsk,
            AskSize = orderBook.BestAskSize
        };
    }

    public async Task<Order> GetOrder(long instrumentId, long orderId)
    {
        if (!_isInitialized)
            throw new ArgumentException($"Cannot get order {orderId} as Exchange has not been initialized");

        if (!_orderBooks.TryGetValue(instrumentId, out var orderBook))
            throw new ArgumentException($"Cannot get order {orderId} as instrumentId {instrumentId} is unknown");

        return await orderBook.GetOrder(orderId);
    }

    /// <summary>
    /// Assume that order has been validated before being submitted to the exchange.
    /// Note: OrderId/Date will be set as part of the Submission Process.
    /// </summary>
    /// <returns>New order Id</returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<Order> SubmitOrder(Order requestedOrder)
    {
        if (!_isInitialized)
            throw new ArgumentException($"Cannot submit order as Exchange has not been initialized.");

        if (!_orderBooks.ContainsKey(requestedOrder.InstrumentId))
            throw new ArgumentException($"Asked to register offer for unknown instrument {requestedOrder.InstrumentId}");

        var newOrder = requestedOrder with
        {
            OrderId = Interlocked.Increment(ref _lastOfferId),
            OrderDate = DateTime.UtcNow,
            OrderStatus = OrderStatus.Active
        };

        // Always record order request, even if unable to service it currently.
        if (newOrder.ClientId > 0)
            await _ordersQuery.SubmitOrder(newOrder);

        _operations.Enqueue(new SubmitOrderOperation
        {
            InstrumentId = newOrder.InstrumentId,
            Order = newOrder
        });

        var returnValue = newOrder with { };

        if (requestedOrder.ClientId != null)
        {
            _messageNexus.FireMessage<OrderUpdateMessage>(new()
            {
                ClientId = requestedOrder.ClientId.Value,
                Order = returnValue?.ToDto()
            });
        }

        return returnValue;
    }

    public async Task CancelOrder(long instrumentId, long orderId)
    {
        if (!_isInitialized)
            throw new ArgumentException($"Cannot cancel order as Exchange has not been initialized.");

        if (!_orderBooks.ContainsKey(instrumentId))
            throw new ArgumentException($"Asked to cancel order for unknown instrument {instrumentId}");

        _operations.Enqueue(new CancelOrderOperation { OrderId = orderId, InstrumentId = instrumentId });
    }

    private async Task Initialize()
    {
        if (_isInitialized)
            throw new InvalidOperationException("Cannot initalize Exchange as exchange is already initialized");

        var allInstruments = (await _instrumentsQuery.GetInstruments()).Where(i => i.InstrumentId != (long)SpecialInstruments.Cash).ToList();
        var existingOrders = (await _ordersQuery.GetOrders(null)).OrderBy(o => o.OrderId);

        _orderBooks = new();
        _operations = new();

        // Load Known Instruments
        _logger.Info("Initializing Exchange");
        _lastOfferId = await _ordersQuery.GetLastOrderId();

        // Register Known Instruments
        foreach (var id in allInstruments.Select(i => i.InstrumentId))
        {
            _orderBooks[id] = new OrderBook(_logger, _ordersQuery, _tradesQuery, _positionsQuery, _ledgerQuery, _messageNexus, _transactionLock)
            {
                InstrumentId = id
            };
        }

        // Replay orders into orderbook
        foreach (var order in existingOrders)
        {
            if (order.OrderStatus != OrderStatus.Active || order.Quantity == order.Filled)
                continue;

            if (!_orderBooks.TryGetValue(order.InstrumentId, out var orderBook))
            {
                _logger.Error("Exchange tried to load order for unknown Instrument {instrumentId}", order.InstrumentId);
                continue;
            }

            await orderBook.HandleSubmitOrder(order);
        }

        _isInitialized = true;
    }

    private void CleanUp()
    {
        _isInitialized = false;
        _orderBooks = null;
        _operations = null;
    }

    protected override void Work()
    {
        var ct = _ct;

        Initialize().Wait();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                ManageOrderBooks(ct).Wait();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error occurred Managing Order Book");
            }
        }

        CleanUp();
    }

    private async Task ManageOrderBooks(CancellationToken ct)
    {
        if (!_operations.TryDequeue(out var operation))
        {
            try
            {
                 await Task.Delay(50, ct);
            }
            catch (Exception) { }
            return;
        }

        var orderBook = _orderBooks.GetValueOrDefault(operation.InstrumentId);

        if (operation is SubmitOrderOperation submit)
            await orderBook.HandleSubmitOrder(submit.Order);
        else if (operation is CancelOrderOperation cancel)
            await orderBook.HandleCancelOrder(cancel.OrderId);
    }
}

internal abstract record OrderBookOperation
{
    public long InstrumentId { get; set; }
};

internal record SubmitOrderOperation : OrderBookOperation
{
    public Order Order { get; set; }
}

internal record CancelOrderOperation : OrderBookOperation
{
    public long OrderId { get; set; }
}