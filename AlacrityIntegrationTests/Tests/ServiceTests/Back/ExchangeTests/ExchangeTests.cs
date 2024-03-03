using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.Back;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Back.ExchangeTests;

public class ExchangeTests
{
    private readonly Order _dummyOrder = new()
    {
        InstrumentId = 1,
        ClientId = null,
        Quantity = 100,
        LimitPrice = 100,
        OrderKind = OrderKind.LimitOrder,
        OrderDirection = TradeDirection.Buy,
        OrderStatus = OrderStatus.Active
    };

    private async
        Task<(Exchange exchange, InstrumentsQuery instrumentsQuery, OrdersQuery ordersQuery, TradesQuery tradesQuery)>
        GetDependencies()
    {
        var mockLogger = new Mock<IALogger>().Object;
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix($"Exchange-{Guid.NewGuid().ToString()[0..8]}");

        var ordersQuery = new OrdersQuery(connection);
        var instrumentsQuery = new InstrumentsQuery(connection);
        var tradesQuery = new TradesQuery(connection);
        var positionsQuery = new PositionsQuery(connection);
        var ledgerQuery = new LedgerQuery(connection);
        var messageNexus = new MessageNexus();

        var exchange = new Exchange(mockLogger, ordersQuery, instrumentsQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, new TransactionLock());

        return (exchange, instrumentsQuery, ordersQuery, tradesQuery);
    }

    [Test]
    public async Task SubmittingOrdersWorks_NoClientId()
    {
        var (exchange, instrumentsQuery, ordersQuery, tradesQuery) = await GetDependencies();

        exchange.Start();
        await Task.Delay(500);

        var orderData = _dummyOrder with { };
        var newOrder = await exchange.SubmitOrder(orderData);

        Assert.That(newOrder.OrderId, Is.GreaterThan(0));

        var newerOrder = await ordersQuery.GetOrder(newOrder.OrderId);
        Assert.That(newerOrder, Is.Null);
    }

    [Test]
    public async Task SubmittingOrdersWorks_WithClientId()
    {
        var (exchange, instrumentsQuery, ordersQuery, tradesQuery) = await GetDependencies();


        exchange.Start();
        await Task.Delay(500);

        // Make sure that clients can be associated with orders too
        var orderData = _dummyOrder with { ClientId = 1 };
        var newOrder = await exchange.SubmitOrder(orderData);

        Assert.That(newOrder.OrderId, Is.GreaterThan(0));

        var newerOrder = await ordersQuery.GetOrder(newOrder.OrderId);
        Assert.Multiple(() =>
        {
            Assert.That(newerOrder, Is.Not.Null);
            Assert.That(newerOrder.InstrumentId, Is.EqualTo(orderData.InstrumentId));
            Assert.That(newerOrder.ClientId, Is.EqualTo(orderData.ClientId));
            Assert.That(newerOrder.Quantity, Is.EqualTo(orderData.Quantity));
            Assert.That(newerOrder.LimitPrice, Is.EqualTo(orderData.LimitPrice));
            Assert.That(newerOrder.OrderKind, Is.EqualTo(orderData.OrderKind));
            Assert.That(newerOrder.OrderDirection, Is.EqualTo(orderData.OrderDirection));
            Assert.That(newerOrder.OrderStatus, Is.EqualTo(OrderStatus.Active)); ;
        });
    }

    [Test]
    public async Task CancellingOrders_Works()
    {
        var (exchange, instrumentsQuery, ordersQuery, tradesQuery) = await GetDependencies();

        exchange.Start();
        await Task.Delay(500);

        // Make sure that clients can be associated with orders too
        var orderData = _dummyOrder with { ClientId = 1 };
        var newOrder = await exchange.SubmitOrder(orderData);
        Assert.That(newOrder.OrderId, Is.GreaterThan(0));

        await exchange.CancelOrder(1, newOrder.OrderId);

        await Task.Delay(500);

        var newOrderFresh = await ordersQuery.GetOrder(newOrder.OrderId);
        Assert.Multiple(() =>
        {
            Assert.That(newOrderFresh.Filled, Is.EqualTo(0));
            Assert.That(newOrderFresh.OrderStatus, Is.EqualTo(OrderStatus.Cancelled));
        });
    }

    [Test]
    public async Task UpdatingOrder_Works()
    {
        var (exchange, instrumentsQuery, ordersQuery, tradesQuery) = await GetDependencies();

        exchange.Start();
        await Task.Delay(500);

        var orderData = _dummyOrder with { ClientId = 1 };
        var newOrder = await exchange.SubmitOrder(orderData);
        Assert.That(newOrder.OrderId, Is.GreaterThan(0));

        await ordersQuery.UpdateOrder(newOrder.OrderId, 100, OrderStatus.Completed);

        var newOrderFresh = await ordersQuery.GetOrder(newOrder.OrderId);
        Assert.That(100, Is.EqualTo(newOrderFresh.Filled));
        Assert.That(OrderStatus.Completed, Is.EqualTo(newOrderFresh.OrderStatus));
    }

    [Test]
    public async Task InitializeOrderBook_WorksCorrectly()
    {
        var (exchange, instrumentsQuery, ordersQuery, tradesQuery) = await GetDependencies();

        var orderData = _dummyOrder with { OrderId = 100, ClientId = 1 };
        await ordersQuery.SubmitOrder(orderData);

        var orderData2 = _dummyOrder with { OrderId = 200, ClientId = 1 };
        await ordersQuery.SubmitOrder(orderData2);

        var orders = await ordersQuery.GetOrders(null); // Null to query all orders
        var activeOrders = orders.Where(o => o.OrderStatus == OrderStatus.Active).ToList();
        Assert.That(2, Is.EqualTo(activeOrders.Count));

        exchange.Start();
        Thread.Sleep(10);
        exchange.Stop();
    }

    [Test]
    public async Task InitializeOrderBook_CompletesMarketableTrades()
    {
        // If there are any marketable pairs of trades in the database,
        // the initialization process will detect these, and complete the trades.
        var (exchange, instrumentsQuery, ordersQuery, tradesQuery) = await GetDependencies();

        var order1 = new Order
        {
            OrderId = 100,
            InstrumentId = 1,
            ClientId = 1,
            Filled = 0,
            OrderDirection = TradeDirection.Buy,
            Quantity = 100,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 100,
            OrderStatus = OrderStatus.Active,
            OrderDate = DateTime.UtcNow,
        };

        var order2 = new Order
        {
            OrderId = 101,
            InstrumentId = 1,
            ClientId = 2,
            Filled = 30,
            OrderDirection = TradeDirection.Sell,
            Quantity = 200,
            OrderKind = OrderKind.LimitOrder,
            LimitPrice = 90,
            OrderStatus = OrderStatus.Active,
            OrderDate = DateTime.UtcNow,
        };

        await ordersQuery.SubmitOrder(order1);
        await ordersQuery.SubmitOrder(order2);


        exchange.Start();
        Thread.Sleep(500);

        var orders1 = await ordersQuery.GetOrders(1);
        var orders2 = await ordersQuery.GetOrders(2);

        var foundOrder1 = orders1.Find(o => o.OrderId == 100);
        var foundOrder2 = orders2.Find(o => o.OrderId == 101);

        Assert.That(foundOrder1, Is.Not.Null);
        Assert.That(foundOrder2, Is.Not.Null);

        Assert.That(OrderStatus.Completed, Is.EqualTo(foundOrder1.OrderStatus));
        Assert.That(OrderStatus.Active, Is.EqualTo(foundOrder2.OrderStatus));

        Assert.That(100, Is.EqualTo(foundOrder1.Filled));
        Assert.That(130, Is.EqualTo(foundOrder2.Filled));

        var trades1 = await tradesQuery.GetTrades(1);
        var trade1 = trades1.Find(t => t.OrderId == 100);
        var trades2 = await tradesQuery.GetTrades(2);
        var trade2 = trades2.Find(t => t.OrderId == 101);
        Assert.That(100, Is.EqualTo(trade1.Quantity));
        Assert.That(100, Is.EqualTo(trade2.Quantity));
    }
}
