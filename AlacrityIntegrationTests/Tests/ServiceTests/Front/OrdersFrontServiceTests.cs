using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.ReqRes.Orders;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;
using AlacrityCore.Services.Front;
using AlacrityCore.Utils;
using Microsoft.Extensions.Logging;
using Moq;

namespace AlacrityIntegrationTests.Tests.ServiceTests.Front;
public class OrdersFrontServiceTests
{
    private const int _clientId = 3;

    private async
        Task<(Exchange exchange, InstrumentsQuery instrumentsQuery, OrdersQuery ordersQuery, OrdersFrontService service)>
        GetDependencies()
    {
        var mockExchangeLogger = new Mock<IALogger>().Object;
        var mockOrderFrontServiceLogger = new Mock<ILogger<OrdersFrontService>>().Object;
        var connection = await IntegrationFixture.SetUpDatabaseWithPrefix($"Orders-{Guid.NewGuid().ToString()[0..8]}");

        var ordersQuery = new OrdersQuery(connection);
        var instrumentQuery = new InstrumentsQuery(connection);
        var tradesQuery = new TradesQuery(connection);
        var positionsQuery = new PositionsQuery(connection);
        var ledgerQuery = new LedgerQuery(connection);
        var messageNexus = new MessageNexus();

        var (cashPosition, averagePrice) = await positionsQuery.AddToPosition(_clientId, -1, 1_000_000, 1);
        Assert.AreEqual(1_000_000, cashPosition);

        var exchange = new Exchange(mockExchangeLogger, ordersQuery, instrumentQuery, tradesQuery, positionsQuery, ledgerQuery, messageNexus, new TransactionLock());
        var service = new OrdersFrontService(mockOrderFrontServiceLogger, ordersQuery, instrumentQuery, positionsQuery, exchange);

        return (exchange, instrumentQuery, ordersQuery, service);
    }

    private static readonly SubmitOrderRequest _dummyRequest = new()
    {
        InstrumentId = 1,
        OrderDirection = TradeDirection.Buy,
        Quantity = 200,
        LimitPrice = 100,
        OrderKind = OrderKind.LimitOrder
    };

    [Test]
    public async Task SubmitOrder_ThrowsForBadConfiguration()
    {
        var (exchange, instrumentsQuery, ordersQuery, service) = await GetDependencies();

        await exchange.Start();
        await TaskUtil.AwaitPredicate(() => exchange.IsInitialized);

        var result1 = await service.SubmitOrder(_clientId, _dummyRequest with { OrderKind = OrderKind.MarketOrder });
        Assert.IsNotNull(result1.FailureReason);

        var result2 = await service.SubmitOrder(_clientId, _dummyRequest with { LimitPrice = null });
        Assert.IsNotNull(result2);

        var result3 = await service.SubmitOrder(_clientId, _dummyRequest with { OrderDirection = TradeDirection.Unknown });
        Assert.IsNotNull(result3);

        var result4 = await service.SubmitOrder(_clientId, _dummyRequest with { Quantity = -100 });
        Assert.IsNotNull(result4);
    }

    [Test]
    public async Task SubmitOrder_WorksForGoodConfiguration()
    {
        var (exchange, instrumentsQuery, ordersQuery, service) = await GetDependencies();

        await exchange.Start();
        await TaskUtil.AwaitPredicate(() => exchange.IsInitialized);

        var order = await service.SubmitOrder(_clientId, _dummyRequest);
        Assert.That(order.Succeeded, Is.EqualTo(true));
    }

    [Test]
    public async Task GetOrders()
    {
        var (exchange, instrumentsQuery, ordersQuery, service) = await GetDependencies();

        await exchange.Start();
        await TaskUtil.AwaitPredicate(() => exchange.IsInitialized);

        await service.SubmitOrder(_clientId, _dummyRequest);
        await service.SubmitOrder(_clientId, _dummyRequest);
        await service.SubmitOrder(_clientId, _dummyRequest);

        var result = await service.GetOrders(_clientId);

        Assert.That(result, Has.Count.EqualTo(3));

        var order = result.First();
        Assert.Multiple(() =>
        {
            Assert.That(order.InstrumentId, Is.EqualTo(1));
            Assert.That(order.OrderDirection, Is.EqualTo(TradeDirection.Buy));
            Assert.That(order.Quantity, Is.EqualTo(200));
            Assert.That(order.LimitPrice, Is.EqualTo(100));
            Assert.That(order.OrderKind, Is.EqualTo(OrderKind.LimitOrder));
        });
    }

    [Test]
    public async Task CancelOrder_InvalidOrder()
    {
        var (exchange, instrumentsQuery, ordersQuery, service) = await GetDependencies();

        await exchange.Start();
        await TaskUtil.AwaitPredicate(() => exchange.IsInitialized);

        var result = await service.CancelOrder(-1, -1);
    }

    [Test]
    public async Task CancelOrder_ValidOrder()
    {
        var (exchange, instrumentsQuery, ordersQuery, service) = await GetDependencies();

        await exchange.Start();
        await TaskUtil.AwaitPredicate(() => exchange.IsInitialized);

        var result = await service.SubmitOrder(_clientId, _dummyRequest);
        Assert.That(result.Succeeded, Is.True);

        var cancellationResult = await service.CancelOrder(_clientId, result.OrderId.Value);

        Thread.Sleep(100);

        var cancelledOrder = (await service.GetOrders(_clientId)).FirstOrDefault();
        Assert.That(cancelledOrder, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(cancelledOrder.OrderId, Is.EqualTo(result.OrderId));
            Assert.That(cancelledOrder.Filled, Is.EqualTo(0));
            Assert.That(cancelledOrder.OrderStatus, Is.EqualTo(OrderStatus.Cancelled));
        });
    }

    [Test]
    public async Task CancelOrder_PreviouslyCancelledOrder()
    {
        var (exchange, instrumentsQuery, ordersQuery, service) = await GetDependencies();

        await exchange.Start();
        await TaskUtil.AwaitPredicate(() => exchange.IsInitialized);

        var result = await service.SubmitOrder(_clientId, _dummyRequest);
        Assert.That(result.Succeeded, Is.True);

        var cancellationResult = await service.CancelOrder(_clientId, result.OrderId.Value);

        Thread.Sleep(100);

        var cancelledOrder = (await service.GetOrders(_clientId)).FirstOrDefault();
        Assert.That(cancelledOrder, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(cancelledOrder.OrderStatus, Is.EqualTo(OrderStatus.Cancelled));
        });

        var secondResult = await service.CancelOrder(_clientId, result.OrderId.Value);
    }
}
