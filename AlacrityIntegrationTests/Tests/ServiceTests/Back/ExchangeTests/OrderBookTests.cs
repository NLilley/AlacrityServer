using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.Back;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back.ExchangeTests;
public class OrderBookTests
{
    private async
    Task<(OrdersQuery ordersQuery, TradesQuery tradesQuery, OrderBook orderBook)>
    GetDependencies()
    {
        var mockLogger = new Mock<IALogger>().Object;
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix($"OrderBook-{Guid.NewGuid().ToString()[0..8]}");
        var ordersQuery = new OrdersQuery(connection);
        var tradesQuery = new TradesQuery(connection);
        var positionsQuery = new PositionsQuery(connection);
        var ledgerQuery = new LedgerQuery(connection);
        var messageNexus = new MessageNexus();

        var orderBook = new OrderBook(mockLogger, ordersQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, new TransactionLock());
        return (ordersQuery, tradesQuery, orderBook);
    }

    private readonly Order _dummyOrder = new()
    {
        OrderId = 100,
        InstrumentId = 1,
        ClientId = 1,
        Filled = 0,
        OrderDirection = TradeDirection.Buy,
        Quantity = 100,
        OrderKind = OrderKind.MarketOrder,
        LimitPrice = null,
        OrderStatus = OrderStatus.Active,
        OrderDate = DateTime.UtcNow,
    };

    [Test]
    public async Task CanSubmitToEmptyOrderBook()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order = _dummyOrder with { };
        await orderBook.HandleSubmitOrder(order);

        var orderInBook = await orderBook.GetOrder(order.OrderId);
        Assert.That(0, Is.EqualTo(orderInBook.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(orderInBook.OrderStatus));
    }

    [Test]
    public async Task MarketOrderInBook_DoesNotFillWhenNewMarketOrdersArrive()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with { OrderId = 100 };
        await orderBook.HandleSubmitOrder(order1);

        var orderInBook1_1 = await orderBook.GetOrder(order1.OrderId);
        Assert.That(0, Is.EqualTo(orderInBook1_1.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(orderInBook1_1.OrderStatus));

        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
        };
        await orderBook.HandleSubmitOrder(order2);

        var orderInBook1_2 = await orderBook.GetOrder(order1.OrderId);
        var orderInBook2_1 = await orderBook.GetOrder(order2.OrderId);

        Assert.That(0, Is.EqualTo(orderInBook1_2.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(orderInBook1_2.OrderStatus));
        Assert.That(0, Is.EqualTo(orderInBook2_1.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(orderInBook2_1.OrderStatus));
    }

    [Test]
    public async Task MarketOrderInBook_FillsWhenLimitOrderAdded()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with { OrderId = 100 };
        await orderBook.HandleSubmitOrder(order1);

        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 200,
            Quantity = 50
        };

        await ordersQuery.SubmitOrder(order1);

        await orderBook.HandleSubmitOrder(order2);

        var orderInBook1_1 = await orderBook.GetOrder(order1.OrderId);
        var orderInBook2_1 = await orderBook.GetOrder(order2.OrderId);

        Assert.That(50, Is.EqualTo(orderInBook1_1.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(orderInBook1_1.OrderStatus));
        Assert.That(orderInBook2_1, Is.Null); // Filled, so removed       
    }

    [Test]
    public async Task LimitOrderInBook_FillsWhenMarketOrderAdded()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with { OrderId = 100 };
        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 200,
            Quantity = 50
        };

        await ordersQuery.SubmitOrder(order1);

        await orderBook.HandleSubmitOrder(order2);
        await orderBook.HandleSubmitOrder(order1);

        var orderInBook1_1 = await orderBook.GetOrder(order1.OrderId);
        var orderInBook2_1 = await orderBook.GetOrder(order2.OrderId);

        Assert.That(50, Is.EqualTo(orderInBook1_1.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(orderInBook1_1.OrderStatus));
        Assert.That(orderInBook2_1, Is.Null); // Filled, so removed
    }

    [Test]
    public async Task LimitOrderInBook_FillsWhenLimitOrderAdded()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with
        {
            OrderId = 100,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 200,
            Quantity = 100
        };
        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 100,
            Quantity = 100
        };

        await ordersQuery.SubmitOrder(order1);

        await orderBook.HandleSubmitOrder(order1);
        await orderBook.HandleSubmitOrder(order2);

        var orderInBook1_1 = await orderBook.GetOrder(order1.OrderId);
        var orderInBook2_1 = await orderBook.GetOrder(order2.OrderId);

        Assert.That(orderInBook1_1, Is.Null); // Filled, so removed
        Assert.That(orderInBook2_1, Is.Null); // Filled, so removed

        var trades = await tradesQuery.GetTrades(1);
        var trade = trades.Find(t => t.OrderId == 100);
        Assert.That(200, Is.EqualTo(trade.Price));
        Assert.That(100, Is.EqualTo(trade.Quantity));
    }

    [Test]
    public async Task OlderOrdersExecuteFirst()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with
        {
            OrderId = 100,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 100,
            Quantity = 100
        };

        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 100,
            Quantity = 100
        };

        var order3 = _dummyOrder with
        {
            OrderId = 102,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 200,
            Quantity = 100
        };

        await orderBook.HandleSubmitOrder(order1);
        await orderBook.HandleSubmitOrder(order2);

        await ordersQuery.SubmitOrder(order3);

        await orderBook.HandleSubmitOrder(order3);

        var firstOrder = await orderBook.GetOrder(100);
        var secondOrder = await orderBook.GetOrder(101);
        Assert.That(firstOrder, Is.Null); // Should have Completed
        Assert.That(secondOrder, Is.Not.Null);
        Assert.That(0, Is.EqualTo(secondOrder.Filled));

        var trades = await tradesQuery.GetTrades(1);
        var trade = trades.Find(t => t.OrderId == 102);
        Assert.That(100, Is.EqualTo(trade.Price));
        Assert.That(100, Is.EqualTo(trade.Quantity));
    }

    [Test]
    public async Task MarketOrder_CanSnagMultipleTradesAtOnce()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with
        {
            OrderId = 100,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.MarketOrder,
            Quantity = 50
        };
        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 100,
            Quantity = 100
        };
        var order3 = _dummyOrder with
        {
            OrderId = 102,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 200,
            Quantity = 150
        };
        var order4 = _dummyOrder with
        {
            OrderId = 103,
            OrderKind = OrderKind.MarketOrder,
            Quantity = 400
        };

        // OrderBook requires orders with clientIds to be in DB before calling HandleSubmitOrder
        await ordersQuery.SubmitOrder(order4);

        await orderBook.HandleSubmitOrder(order1);
        await orderBook.HandleSubmitOrder(order2);
        await orderBook.HandleSubmitOrder(order3);
        await orderBook.HandleSubmitOrder(order4);

        var allTrades = await tradesQuery.GetTrades(order4.ClientId.Value);
        var orderTrades = allTrades.Where(t => t.OrderId == order4.OrderId).OrderBy(o => o.TradeId).ToList();
        Assert.That(3, Is.EqualTo(orderTrades.Count));

        var remainingOrder = await orderBook.GetOrder(order4.OrderId);
        Assert.That(300, Is.EqualTo(remainingOrder.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(remainingOrder.OrderStatus));

        var trade1 = orderTrades[0];
        var trade2 = orderTrades[1];
        var trade3 = orderTrades[2];
        Assert.That(100, Is.EqualTo(trade1.Price));
        Assert.That(50, Is.EqualTo(trade1.Quantity));
        Assert.That(100, Is.EqualTo(trade2.Price));
        Assert.That(100, Is.EqualTo(trade2.Quantity));
        Assert.That(200, Is.EqualTo(trade3.Price));
        Assert.That(150, Is.EqualTo(trade3.Quantity));
    }

    [Test]
    public async Task LimitOrder_CanSnagMultipleTradesAtOnce()
    {
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with
        {
            OrderId = 100,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.MarketOrder,
            Quantity = 50
        };
        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 100,
            Quantity = 100
        };
        var order3 = _dummyOrder with
        {
            OrderId = 102,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 200,
            Quantity = 150
        };
        var order4 = _dummyOrder with
        {
            OrderId = 103,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 150,
            Quantity = 400
        };

        // OrderBook requires orders with clientIds to be in DB before calling HandleSubmitOrder
        await ordersQuery.SubmitOrder(order4);

        await orderBook.HandleSubmitOrder(order1);
        await orderBook.HandleSubmitOrder(order2);
        await orderBook.HandleSubmitOrder(order3);
        await orderBook.HandleSubmitOrder(order4);

        var allTrades = await tradesQuery.GetTrades(order4.ClientId.Value);
        var orderTrades = allTrades.Where(t => t.OrderId == order4.OrderId).OrderBy(t => t.TradeId).ToList();
        Assert.That(2, Is.EqualTo(orderTrades.Count));

        var remainingOrder = await orderBook.GetOrder(order4.OrderId);
        Assert.That(150, Is.EqualTo(remainingOrder.Filled));
        Assert.That(OrderStatus.Active, Is.EqualTo(remainingOrder.OrderStatus));

        var trade1 = orderTrades[0];
        var trade2 = orderTrades[1];
        Assert.That(150, Is.EqualTo(trade1.Price));
        Assert.That(50, Is.EqualTo(trade1.Quantity));
        Assert.That(100, Is.EqualTo(trade2.Price));
        Assert.That(100, Is.EqualTo(trade2.Quantity));
    }

    [Test]
    public async Task MarketOrder_CanExecuteWhenMixedCounterOrders()
    {
        // TODO: There's a quirk in my implementation
        // This test should cause two trades, but currently only causes one.
        var (ordersQuery, tradesQuery, orderBook) = await GetDependencies();

        var order1 = _dummyOrder with
        {
            OrderId = 100,
            OrderKind = OrderKind.MarketOrder,
            Quantity = 200
        };
        var order2 = _dummyOrder with
        {
            OrderId = 101,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.MarketOrder,
            Quantity = 100
        };
        var order3 = _dummyOrder with
        {
            OrderId = 102,
            ClientId = null,
            OrderDirection = TradeDirection.Sell,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 100,
            Quantity = 100
        };

        await ordersQuery.SubmitOrder(order1);
        await orderBook.HandleSubmitOrder(order1);
        await orderBook.HandleSubmitOrder(order2);
        await orderBook.HandleSubmitOrder(order3);

        var allTrades = await tradesQuery.GetTrades(order1.ClientId.Value);
        var orderTrades = allTrades.Where(t => t.OrderId == order1.OrderId).ToList();
        Assert.That(2, Is.EqualTo(orderTrades.Count));
    }
}
