using AlacrityCore.Models.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace AlacrityCore.Infrastructure;

public record SubscribeInstrumentUpdatesRequest
{
    public long[] InstrumentIds { get; set; }
}
public record UnsubscribeInstrumentUpdatesRequest
{
    public long[] InstrumentIds { get; set; }
}

public record SubscribePositionUpdatesRequest { }
public record UnsubscribePositionUpdatesRequest { }

public record SubscribeOrderUpdatesRequest { }
public record UnsubscribeOrderUpdatesRequest { }

public record SubscribeAccountSummaryUpdates { }
public record UnsubscribeAccountSummaryUpdates { }

public interface ICentralHubClient
{
    Task ReceivePriceUpdate(QuoteDto quote);
    Task ReceiveIndicatorUpdate(InstrumentIndicatorDto indicator);
    Task ReceivePositionUpdate(PositionDto position);
    Task ReceiveOrderUpdate(OrderDto order);
    Task ReceiveAccountSummaryUpdate(AccountSummaryDto accountUpdate);
}

public interface ICentralHubServer
{
    Task SubscribeInstrumentUpdates(SubscribeInstrumentUpdatesRequest request);
    Task UnsubscribeInstrumentUpdates(UnsubscribeInstrumentUpdatesRequest request);
    Task SubscribePositionUpdates(SubscribePositionUpdatesRequest request);
    Task UnsubscribePositionUpdates(UnsubscribeInstrumentUpdatesRequest request);
    Task SubscribeOrderUpdates(SubscribeOrderUpdatesRequest request);
    Task UnsubscribeOrderUpdates(UnsubscribeOrderUpdatesRequest request);
    Task SubscribeAccountSummaryUpdates(SubscribeAccountSummaryUpdates request);
    Task UnsubscribeAccountSummaryUpdates(UnsubscribeAccountSummaryUpdates request);
}

public static class CentralHubHelper
{
    public static readonly string _instrumentGroupPrefix = "instrument";
    public static string InstrumentGroupName(long instrumentId) => $"{_instrumentGroupPrefix}:{instrumentId}";

    public static readonly string _positionGroupPrefix = "positons";
    public static string PositionGroupName(int clientId) => $"{_positionGroupPrefix}:{clientId}";

    public static readonly string _orderGroupPrefix = "order";
    public static string OrderGroupName(int clientId) => $"{_orderGroupPrefix}:{clientId}";

    public static readonly string _accountSummaryPrefix = "accountSummary";
    public static string AccountSumaryGroupName(int clientId) => $"{_accountSummaryPrefix}:{clientId}";
}