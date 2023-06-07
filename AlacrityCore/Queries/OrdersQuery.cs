using AlacrityCore.Enums.Order;
using AlacrityCore.Models.Back;
using Dapper;
using Microsoft.Data.Sqlite;

namespace AlacrityCore.Queries;

internal interface IOrdersQuery
{
    Task<long> GetLastOrderId();
    Task<Order> GetOrder(long orderId);
    Task<List<Order>> GetOrders(int? clientId);
    Task SubmitOrder(Order newOrder);
    Task<(bool succeeded, OrderStatus status)> CancelOrder(long orderId);
    Task UpdateOrder(long orderId, decimal filled, OrderStatus status);
}

internal class OrdersQuery : IOrdersQuery
{
    private readonly SqliteConnection _connection;
    public OrdersQuery(SqliteConnection connection)
        => (_connection) = (connection);

    public async Task<long> GetLastOrderId()
        => _connection.QueryFirstOrDefault<long>(
            @"SELECT order_id
              FROM orders
              ORDER BY order_id DESC
              LIMIT 1"
        );

    public async Task<Order> GetOrder(long orderId)
        => _connection.Query<Order>(
            @"SELECT
                order_id                    AS OrderId,
                instrument_id               AS InstrumentId,
                client_id                   AS ClientId,
                order_date                  AS OrderDate,
                order_kind                  AS OrderKind,
                order_status                AS OrderStatus,
                order_direction             AS OrderDirection,
                limit_price                 AS LimitPrice, 
                quantity                    AS Quantity,
                filled                      AS Filled
            FROM orders o
            WHERE o.order_id = @OrderId",
            new { OrderId = orderId }
        ).FirstOrDefault();

    /// <summary>
    /// Note - Pass in null to get all orders for the entire platform.
    /// </summary>        
    public async Task<List<Order>> GetOrders(int? clientId)
        => _connection.Query<Order>(
            @"SELECT
                order_id                    AS OrderId,
                instrument_id               AS InstrumentId,
                client_id                   AS ClientId,
                order_date                  AS OrderDate,
                order_kind                  AS OrderKind,
                order_status                AS OrderStatus,
                order_direction             AS OrderDirection,
                limit_price                 AS LimitPrice, 
                quantity                    AS Quantity,
                filled                      AS Filled
            FROM orders o
            WHERE o.client_id = @ClientId or NULL IS @ClientId",
            new { ClientId = clientId }
        ).ToList();

    public async Task SubmitOrder(Order newOrder)
        => _connection.Execute(@"
            INSERT INTO orders
                (order_date, client_id, order_id, instrument_id, order_kind, order_status, order_direction, limit_price, quantity, filled)
            VALUES
                (@OrderDate, @ClientId, @OrderId, @InstrumentId, @OrderKind, @OrderStatus, @OrderDirection, @LimitPrice, @Quantity, @Filled)
            ", new
        {
            OrderId = newOrder.OrderId,
            OrderDate = newOrder.OrderDate,
            ClientId = newOrder.ClientId,
            InstrumentId = newOrder.InstrumentId,
            OrderKind = newOrder.OrderKind,
            OrderStatus = newOrder.OrderStatus,
            OrderDirection = newOrder.OrderDirection,
            LimitPrice = newOrder.LimitPrice,
            Quantity = newOrder.Quantity,
            Filled = newOrder.Filled
        });

    public async Task<(bool succeeded, OrderStatus status)> CancelOrder(long orderId)
    {
        var result = _connection.QueryMultiple(
            @"UPDATE orders 
            SET order_status = @Cancelled
            WHERE order_id = @OrderId AND order_status = @Active
            RETURNING 1;
            
            SELECT order_status FROM orders WHERE order_id = @OrderId;
            ",
            new { Cancelled = OrderStatus.Cancelled, Active = OrderStatus.Active, OrderId = orderId }
        );

        var succeeded = result.ReadFirstOrDefault<bool?>() ?? false;
        var lastStatus = result.ReadFirstOrDefault<OrderStatus>();

        return (succeeded, lastStatus);
    }

    public async Task UpdateOrder(long orderId, decimal filled, OrderStatus status)
        => _connection.Execute(@"
            UPDATE orders
            SET 
                filled       = @Filled,
                order_status = @Status
            WHERE order_id = @OrderId
        ",
        new
        {
            OrderId = orderId,
            Filled = filled,
            Status = status
        });
}
