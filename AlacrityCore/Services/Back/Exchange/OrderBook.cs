using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.Back;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using System.Collections.Concurrent;
using System.Transactions;

namespace AlacrityCore.Services.Back.Exchange;

internal class OrderBook
{
    private IALogger _logger;
    private IOrdersQuery _ordersQuery;
    private ITradesQuery _tradesQuery;
    private IPositionsQuery _positionsQuery;
    private ILedgerQuery _ledgerQuery;
    private IMessageNexus _messageNexus;
    private ITransactionLock _transactionLock;
    internal OrderBook(
        IALogger logger,
        IOrdersQuery ordersQuery,
        ITradesQuery tradesQuery,
        IPositionsQuery positionsQuery,
        ILedgerQuery ledgerQuery,
        IMessageNexus messageNexus,
        ITransactionLock transactionLock
    )
        => (_logger, _ordersQuery, _tradesQuery, _positionsQuery, _ledgerQuery, _messageNexus, _transactionLock)
        = (logger, ordersQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, transactionLock);

    internal long InstrumentId { get; set; }
    internal decimal? BestBid { get; set; }
    internal decimal? BestBidSize { get; set; }
    internal decimal? BestAsk { get; set; }
    internal decimal? BestAskSize { get; set; }

    private ConcurrentDictionary<long, Order> Orders { get; set; } = new();

    internal async Task<Order> GetOrder(long orderId)
    {
        var order = Orders.GetValueOrDefault(orderId);
        return order == null ? null : order with { };
    }

    internal async Task HandleSubmitOrder(Order order)
    {
        // Order MarketOrders first, then by price, then by order Id to build up order book.
        var allOrders = Orders.Values.ToArray();
        var counterOrders = new List<Order>(allOrders.Length / 2);
        var sameDirectionMarketOrders = new List<Order>();
        decimal? bestMarketPrice = null;
        for (var i = 0; i < allOrders.Length; i++)
        {
            var oldOrder = allOrders[i];
            if (order.OrderDirection != oldOrder.OrderDirection)
            {
                counterOrders.Add(oldOrder);
                if (oldOrder.OrderKind != OrderKind.MarketOrder && bestMarketPrice == null)
                    bestMarketPrice = oldOrder.LimitPrice;
            }
            else if (oldOrder.OrderKind == OrderKind.MarketOrder)
                sameDirectionMarketOrders.Add(oldOrder);
        }

        counterOrders.Sort(order.OrderDirection == TradeDirection.Buy ? BuyComparison : SellComparison);

        var hasTraded = await ScanForCompatibleOrders(order, counterOrders, bestMarketPrice);
        if (hasTraded)
        {
            // Possible that some old MarketOrders can also be completed - so Scan these too.
            sameDirectionMarketOrders.Sort((a, b) => a.OrderId.CompareTo(b.OrderId));
            for (var i = 0; i < sameDirectionMarketOrders.Count; i++)
            {
                var marketOrder = sameDirectionMarketOrders[i];
                if (!await ScanForCompatibleOrders(marketOrder, counterOrders, order.LimitPrice))
                    break;
            }
        }

        // Trade not filled - add it to open orders
        if (order.OrderStatus == OrderStatus.Active)
            Orders.TryAdd(order.OrderId, order);

        var oldQuote = new QuoteDto()
        {
            InstrumentId = InstrumentId,
            Bid = BestBid,
            BidSize = BestBidSize,
            Ask = BestAsk,
            AskSize = BestAskSize
        };

        var orderValues = Orders.Values.ToArray();
        var bestBid = orderValues.Where(o => o.OrderDirection == TradeDirection.Buy).OrderByDescending(o => o.LimitPrice ?? decimal.MinValue).ThenBy(o => o.OrderId).FirstOrDefault();
        BestBid = bestBid?.LimitPrice;
        BestBidSize = bestBid?.Quantity;

        var bestAsk = orderValues.Where(o => o.OrderDirection == TradeDirection.Sell).OrderBy(o => o.LimitPrice ?? decimal.MaxValue).ThenBy(o => o.OrderId).FirstOrDefault();
        BestAsk = bestAsk?.LimitPrice;
        BestAskSize = bestAsk?.Quantity;

        var newQuote = new QuoteDto()
        {
            InstrumentId = InstrumentId,
            Bid = BestBid,
            BidSize = BestBidSize,
            Ask = BestAsk,
            AskSize = BestAskSize
        };

        if (newQuote != oldQuote)
        {
            _messageNexus.FireMessage(new PriceUpdateMessage
            {
                LatestQuote = new()
                {
                    InstrumentId = InstrumentId,
                    Bid = BestBid,
                    BidSize = BestBidSize,
                    Ask = BestAsk,
                    AskSize = BestAskSize
                }
            });
        }
    }

    internal async Task HandleCancelOrder(long orderId)
    {
        var order = Orders.GetValueOrDefault(orderId);
        if (order == null)
        {
            // Possibly Filled!
            _logger.Error("Asked to cancel unknown order {orderId}", orderId);
            return;
        }

        if (order.ClientId != null)
        {
            var (succeeded, status) = await _ordersQuery.CancelOrder(orderId);
            if (!succeeded)
                _logger.Error("OrderBook tried to cancel order {orderId} but cancellation failed. Order Status {status}", orderId, status);
        }

        order.OrderStatus = OrderStatus.Cancelled;
        if (order.ClientId != null)
        {
            _messageNexus.FireMessage<OrderUpdateMessage>(new()
            {
                ClientId = order.ClientId.Value,
                Order = order.ToDto()
            });
        }

        Orders.TryRemove(orderId, out var _);
    }

    private async Task<bool> ScanForCompatibleOrders(Order order, List<Order> counterOrders, decimal? bestMarketPrice)
    {
        var hasTraded = false;
        for (var i = 0; i < counterOrders.Count; i++)
        {
            var counterOrder = counterOrders[i];

            if (order.OrderStatus == OrderStatus.Completed || order.OrderStatus == OrderStatus.Cancelled)
                break;

            if (counterOrder.OrderStatus == OrderStatus.Completed || counterOrder.OrderStatus == OrderStatus.Cancelled)
                continue;

            // Disallow clients from trading with themselves
            if (order.ClientId.HasValue && order.ClientId == counterOrder.ClientId)
                continue;

            decimal? priceToTrade;
            if (order.OrderKind == OrderKind.MarketOrder && counterOrder.OrderKind == OrderKind.MarketOrder)
            {
                // Cannot trade market orders against each other without a price.
                // Use the best price we could find to perform transaction.
                if (bestMarketPrice == null)
                    break;

                priceToTrade = bestMarketPrice.Value;
            }
            else
            {
                if (order.OrderKind == OrderKind.LimitOrder && counterOrder.OrderKind == OrderKind.LimitOrder)
                {
                    // As both are limit orders, we need to make sure that prices are crossed.
                    if (
                        (order.OrderDirection == TradeDirection.Buy && order.LimitPrice < counterOrder.LimitPrice) ||
                        (order.OrderDirection == TradeDirection.Sell && order.LimitPrice > counterOrder.LimitPrice)
                    )
                    {
                        // All market orders dealt with, and limit orders imcompatible.
                        // There cannot be any other compatible trades.
                        // Exit early
                        break;
                    }

                    // Must be as good or better than order's price
                    priceToTrade = counterOrder.LimitPrice;
                }
                else
                {
                    priceToTrade = order.OrderKind == OrderKind.MarketOrder ? counterOrder.LimitPrice : order.LimitPrice;
                }
            }

            // If we get to this point, priceToTrade must have a value, and a trade must be possible.
            var now = DateTime.UtcNow;
            var quantityToTrade = Math.Min(order.Quantity - order.Filled, counterOrder.Quantity - counterOrder.Filled);

            var newOrderFilled = order.Filled + quantityToTrade;
            var newOrderStatus = newOrderFilled == order.Quantity ? OrderStatus.Completed : OrderStatus.Active;

            var newCounterOrderFilled = counterOrder.Filled + quantityToTrade;
            var newCounterOrderStatus = newCounterOrderFilled == counterOrder.Quantity ? OrderStatus.Completed : OrderStatus.Active;

            using (var transaction = new TransactionScope())
            {
                var positionChange = quantityToTrade * (order.OrderDirection == TradeDirection.Buy ? 1 : -1);
                var cashChange = quantityToTrade * priceToTrade * (order.OrderDirection == TradeDirection.Buy ? -1 : 1);

                if (order.ClientId != null)
                {
                    await WriteOrderRows(
                        tradeDate: now,
                        clientId: order.ClientId.Value,
                        orderId: order.OrderId,
                        instrumentId: order.InstrumentId,
                        tradeDirection: order.OrderDirection,
                        quantity: quantityToTrade,
                        price: priceToTrade.Value,
                        newFilled: newOrderFilled,
                        newOrderStatus: newOrderStatus,
                        cashChange: cashChange.Value,
                        originalOrder: order
                    );
                }
                _logger.Info("Order {orderId} Client {orderClientId} Instrument {instrumentId} {direction} {quantity} @ {price}", order.OrderId, order.ClientId, order.InstrumentId, order.OrderDirection.ToString(), order.Quantity, priceToTrade.Value);

                if (counterOrder.ClientId != null)
                {
                    await WriteOrderRows(
                        tradeDate: now,
                        clientId: counterOrder.ClientId.Value,
                        orderId: counterOrder.OrderId,
                        instrumentId: counterOrder.InstrumentId,
                        tradeDirection: counterOrder.OrderDirection,
                        quantity: quantityToTrade,
                        price: priceToTrade.Value,
                        newFilled: newCounterOrderFilled,
                        newOrderStatus: newCounterOrderStatus,
                        cashChange: -cashChange.Value,
                        originalOrder: counterOrder
                    );
                }
                _logger.Info("Order {orderId} Client {orderClientId} Instrument{instrumentId} {direction} {quantity} @ {price}", counterOrder.OrderId, counterOrder.ClientId, counterOrder.OrderDirection.ToString(), counterOrder.Quantity, priceToTrade.Value);

                transaction.Complete();
            }

            hasTraded = true;

            PostTradeOrderProcessing(counterOrder, newCounterOrderStatus, newCounterOrderFilled);
            PostTradeOrderProcessing(order, newOrderStatus, newOrderFilled);
        }

        return hasTraded;
    }

    private async Task WriteOrderRows(
        DateTime tradeDate,
        int clientId,
        long orderId,
        long instrumentId,
        TradeDirection tradeDirection,
        decimal quantity,
        decimal price,
        decimal newFilled,
        OrderStatus newOrderStatus,
        decimal cashChange,
        Order originalOrder
    )
    {
        var locker = _transactionLock.GetLocker(clientId);

        Trade newTrade;
        decimal averageInstrumentPrice;
        decimal newInstrumentPosition;
        decimal newCashPosition;
        lock (locker)
        {
            (newInstrumentPosition, averageInstrumentPrice) = _positionsQuery.AddToPosition(
                clientId, instrumentId, (tradeDirection == TradeDirection.Sell ? -1 : 1) * quantity, price
            ).Result;

            newTrade = new Trade()
            {
                ClientId = clientId,
                InstrumentId = instrumentId,
                OrderId = orderId,
                Price = price,
                Quantity = quantity,
                TradeDirection = tradeDirection,
                TradeDate = tradeDate,
                Profit = tradeDirection == TradeDirection.Sell ? (price - averageInstrumentPrice) * quantity : null
            };

            var newTradeId = _tradesQuery.AddTrade(newTrade).Result;
            newTrade.TradeId = newTradeId;

            _ordersQuery.UpdateOrder(orderId, newFilled, newOrderStatus).Wait();
            (newCashPosition, _) = _positionsQuery.AddToPosition(
                clientId, (long)SpecialInstruments.Cash, cashChange, 1
            ).Result;
            _ledgerQuery.AddEntry(clientId, instrumentId, TransactionKind.Trade, cashChange).Wait();
        }

        _logger.Info("Added trade {trade}", newTrade);

        _messageNexus.FireMessage(new PositionUpdateMessage
        {
            ClientId = clientId,
            Position = new PositionDto
            {
                InstrumentId = instrumentId,
                Quantity = newInstrumentPosition
            }
        });
        _messageNexus.FireMessage(new PositionUpdateMessage
        {
            ClientId = clientId,
            Position = new PositionDto
            {
                InstrumentId = (long)SpecialInstruments.Cash,
                Quantity = newCashPosition
            }
        });
        _messageNexus.FireMessage(new TradeExecutedMessage
        {
            ClientId = clientId,
            Trade = newTrade.ToDto()
        });
    }

    void PostTradeOrderProcessing(Order order, OrderStatus newOrderStatus, decimal newOrderFilled)
    {
        order.Filled = newOrderFilled;
        order.OrderStatus = newOrderStatus;
        if (order.ClientId != null)
        {
            _messageNexus.FireMessage(new OrderUpdateMessage
            {
                ClientId = order.ClientId.Value,
                Order = order.ToDto()
            });
        }

        if (newOrderStatus == OrderStatus.Completed)
            Orders.TryRemove(order.OrderId, out var _);
    }

    private int BuyComparison(Order a, Order b)
    {
        if (a.OrderKind == OrderKind.MarketOrder && b.OrderKind == OrderKind.MarketOrder)
            return a.OrderId.CompareTo(b.OrderId);
        else if (a.OrderKind == OrderKind.MarketOrder)
            return -1;
        else if (b.OrderKind == OrderKind.MarketOrder)
            return 1;
        else
        {
            var comparison = a.LimitPrice.Value.CompareTo(b.LimitPrice.Value);
            return comparison == 0
                ? a.OrderId.CompareTo(b.OrderId)
                : comparison;
        }
    }

    private int SellComparison(Order a, Order b)
    {
        if (a.OrderKind == OrderKind.MarketOrder && b.OrderKind == OrderKind.MarketOrder)
            return a.OrderId.CompareTo(b.OrderId);
        else if (a.OrderKind == OrderKind.MarketOrder)
            return -1;
        else if (b.OrderKind == OrderKind.MarketOrder)
            return 1;
        else
        {
            var comparison = b.LimitPrice.Value.CompareTo(a.LimitPrice.Value);
            return comparison == 0
                ? a.OrderId.CompareTo(b.OrderId)
                : comparison;
        }
    }
}

public class PriceUpdateMessage : MessageNexusMessage
{
    public QuoteDto LatestQuote { get; set; }
}

public class PositionUpdateMessage : MessageNexusMessage
{
    public int ClientId { get; set; }
    public PositionDto Position { get; set; }
}

public class OrderUpdateMessage : MessageNexusMessage
{
    public int ClientId { get; set; }
    public OrderDto Order { get; set; }
}

public class TradeExecutedMessage : MessageNexusMessage
{
    public int ClientId { get; set; }
    public TradeDto Trade { get; set; }
}