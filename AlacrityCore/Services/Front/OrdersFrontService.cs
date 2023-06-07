using AlacrityCore.Enums;
using AlacrityCore.Enums.Order;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Models.ReqRes.Orders;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;
using Microsoft.Extensions.Logging;

namespace AlacrityCore.Services.Front;

public interface IOrdersFrontService
{
    Task<List<OrderDto>> GetOrders(int clientId);
    Task<CancelOrderResponse> CancelOrder(int clientId, long orderId);
    Task<SubmitOrderResponse> SubmitOrder(int clientId, SubmitOrderRequest request);
}

internal class OrdersFrontService : IOrdersFrontService
{
    private readonly ILogger<OrdersFrontService> _logger;
    private readonly IOrdersQuery _ordersQuery;
    private readonly IInstrumentsQuery _instrumentsQuery;
    private readonly IPositionsQuery _positionQuery;
    private readonly IExchange _exchange;
    public OrdersFrontService(
        ILogger<OrdersFrontService> logger,
        IOrdersQuery query,
        IInstrumentsQuery instrumentsQuery,
        IPositionsQuery positionsQuery,
        IExchange exchange
    ) => (_logger, _ordersQuery, _instrumentsQuery, _positionQuery, _exchange) = (logger, query, instrumentsQuery, positionsQuery, exchange);

    public async Task<List<OrderDto>> GetOrders(int clientId)
        => (await _ordersQuery.GetOrders(clientId)).Select(o => o.ToDto()).ToList();

    public async Task<CancelOrderResponse> CancelOrder(int clientId, long orderId)
    {
        var order = await _ordersQuery.GetOrder(orderId);
        if (order == null || order.ClientId != clientId)
        {
            _logger?.LogError("Client: {clientid} is trying to cancel order: {orderId} which belongs to {ownerClientId}",
                clientId, orderId, order?.ClientId);
            return new CancelOrderResponse { };
        }

        if (order.OrderStatus != OrderStatus.Active)
        {
            _logger?.LogError("Client: {clientid} is trying to cancel order: {orderId} which is in an uncancellable state ({orderStatus})",
                clientId, orderId, order.OrderStatus
            );
            return new CancelOrderResponse { };
        }

        await _exchange.CancelOrder(order.InstrumentId, orderId);
        return new() { };
    }

    public async Task<SubmitOrderResponse> SubmitOrder(int clientId, SubmitOrderRequest request)
    {
        try
        {
            await ValidateOrderSubmission(clientId, request);
        }
        catch (Exception ex)
        {
            return new()
            {
                FailureReason = "Invalid Order - Please Contact Alacrity Support if you need assistance"
            };
        }

        // Note: This funds check isn't threadsafe, nor will it gaurantee the client will keep a postive balance!
        var clientPositions = (await _positionQuery.GetPositions(clientId));

        var clientBalance = clientPositions.FirstOrDefault(p => p.InstrumentId == (long)SpecialInstruments.Cash)
            ?.Quantity ?? 0;
        var clientOpenOrders = await _ordersQuery.GetOrders(clientId);
        var marginUsage = clientOpenOrders
            .Select(o => (o.InstrumentId, o.OrderDirection, o.OrderKind, o.LimitPrice, o.Quantity, o.Filled))
            .Append((request.InstrumentId, request.OrderDirection, request.OrderKind, request.LimitPrice, request.Quantity, 0))
            .Select(o =>
            {
                if (o.OrderDirection == TradeDirection.Sell)
                    return 0; // We're not letting clients go "short" currently.

                decimal price;
                if (o.OrderKind == OrderKind.MarketOrder)
                {
                    var latestPrice = _exchange.GetQuote(o.InstrumentId).Result; // 🤫
                    price = (o.OrderDirection == TradeDirection.Buy ? latestPrice.Ask : latestPrice.Bid) ?? 1_000_000;
                }
                else
                    price = o.LimitPrice.Value;

                return price * (o.Quantity - o.Filled);
            }).Sum();

        if (marginUsage > clientBalance)
        {
            _logger.LogInformation("Client {clientId} trying to place a request which exceeds their margin: {marginUsage} > {clientBalance}", clientId, marginUsage, clientBalance);
            return new()
            {
                FailureReason = "Margin Exceeded - Please Contact Alacrity Support if you need assistance"
            };
        }

        if (request.OrderDirection == TradeDirection.Sell)
        {
            var instrumentBalance = clientPositions.FirstOrDefault(p => p.InstrumentId == request.InstrumentId)?.Quantity ?? 0;
            if (request.Quantity > instrumentBalance)
            {
                _logger.LogInformation("Client {clientId} trying to place a sell larger than their position: {request} > {balance}", clientId, request.Quantity, instrumentBalance);
                return new()
                {
                    FailureReason = "Sale Exceeds Position - Please Contact Alacrity Support if you need assistance"
                };
            }

        }

        var newOrder = await _exchange.SubmitOrder(new()
        {
            ClientId = clientId,
            InstrumentId = request.InstrumentId,
            OrderKind = request.OrderKind,
            OrderDirection = request.OrderDirection,
            Quantity = request.Quantity,
            LimitPrice = request.LimitPrice,
        });

        return new()
        {
            OrderId = newOrder.OrderId,
            Succeeded = true
        };
    }

    private static readonly HashSet<OrderKind> _validClientOrderKinds = new() { OrderKind.LimitOrder, OrderKind.MarketOrder };
    private static readonly HashSet<TradeDirection> _validTradeDirections = new() { TradeDirection.Buy, TradeDirection.Sell };
    private async Task ValidateOrderSubmission(int clientId, SubmitOrderRequest request)
    {
        try
        {
            var instrument = await _instrumentsQuery.GetInstrument(request.InstrumentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client {clientId} tried to submit an order for invalid instrument {instrumentId}", clientId, request.InstrumentId);
            Throw();
        }

        if (
            !_validClientOrderKinds.Contains(request.OrderKind)
            || !_validTradeDirections.Contains(request.OrderDirection)
            || (request.OrderKind == OrderKind.LimitOrder && (request.LimitPrice ?? 0) <= 0)
            || (request.OrderKind == OrderKind.MarketOrder && (request.LimitPrice != null))
            || (request.Quantity <= 0)
            || (request.InstrumentId <= 0)
        )
        {
            _logger.LogError("Client {clientId} tried to submit an order with an invalid OrderKind: {request}", clientId, request);
            Throw();
        }

        static void Throw() => throw new ArgumentException("Invalid Request");
    }
}
