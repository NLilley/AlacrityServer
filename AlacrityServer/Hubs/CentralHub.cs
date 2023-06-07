using AlacrityCore.Enums;
using AlacrityCore.Infrastructure;
using AlacrityCore.Services.Back.Aggregation;
using AlacrityCore.Services.Front;
using AlacrityServer.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AlacrityServer.Hubs;

[Authorize]
public class CentralHub : Hub<ICentralHubClient>, ICentralHubServer
{
    private IInstrumentFrontService _instrumentFrontService;
    private IOrdersFrontService _ordersFrontService;
    private IPositionsFrontService _positionsFrontService;
    private IAccountSummaryAggregator _accountSummaryAggregator;
    public CentralHub(
        IInstrumentFrontService instrumentFrontService,
        IOrdersFrontService ordersFrontService,
        IPositionsFrontService positionsFrontService,
        IAccountSummaryAggregator accountSummaryAggregator
    )
    {
        _instrumentFrontService = instrumentFrontService;
        _ordersFrontService = ordersFrontService;
        _positionsFrontService = positionsFrontService;
        _accountSummaryAggregator = accountSummaryAggregator;
    }

    public async Task SubscribeInstrumentUpdates(SubscribeInstrumentUpdatesRequest request)
    {
        foreach (var instrumentId in request.InstrumentIds ?? Array.Empty<long>())
        {
            if (instrumentId <= 0)
                continue;

            var groupName = CentralHubHelper.InstrumentGroupName(instrumentId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var latestQuote = await _instrumentFrontService.GetQuote(instrumentId);
            await Clients.Client(Context.ConnectionId).ReceivePriceUpdate(latestQuote);

            var latestIndicators = await _instrumentFrontService.GetIndicators(instrumentId);
            foreach (var (_, indicator) in latestIndicators)
                await Clients.Client(Context.ConnectionId).ReceiveIndicatorUpdate(indicator);
        }
    }

    public async Task UnsubscribeInstrumentUpdates(UnsubscribeInstrumentUpdatesRequest request)
    {
        foreach (var instrumentId in request.InstrumentIds ?? Array.Empty<long>())
        {
            if (instrumentId <= 0)
                continue;

            var groupName = CentralHubHelper.InstrumentGroupName(instrumentId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }

    public async Task SubscribePositionUpdates(SubscribePositionUpdatesRequest request)
    {
        var clientId = Context.GetHttpContext().GetClientId();
        if (clientId <= 0)
            throw new ArgumentException($"Unable to subscribe for positions as clientId {clientId} is invalid");

        var groupName = CentralHubHelper.PositionGroupName(clientId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var existingPositions = await _positionsFrontService.GetPositions(clientId);
        foreach (var position in existingPositions)
        {
            await Clients.Client(Context.ConnectionId).ReceivePositionUpdate(position);
        }
    }

    public async Task UnsubscribePositionUpdates(UnsubscribeInstrumentUpdatesRequest request)
    {
        var clientId = Context.GetHttpContext().GetClientId();
        if (clientId <= 0)
            throw new ArgumentException($"Unable to unsubscribe from positions as clientId {clientId} is invalid");

        var groupName = CentralHubHelper.PositionGroupName(clientId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SubscribeOrderUpdates(SubscribeOrderUpdatesRequest request)
    {
        var clientId = Context.GetHttpContext().GetClientId();
        if (clientId <= 0)
            throw new ArgumentException($"Unable to subscribe to orders as clientId {clientId} is invalid");

        var groupName = CentralHubHelper.OrderGroupName(clientId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        var exisitingOrders = (await _ordersFrontService.GetOrders(clientId)).Where(o => o.OrderStatus == AlacrityCore.Enums.Order.OrderStatus.Active);
        foreach (var order in exisitingOrders)
        {
            await Clients.Client(Context.ConnectionId).ReceiveOrderUpdate(order);
        }
    }

    public async Task UnsubscribeOrderUpdates(UnsubscribeOrderUpdatesRequest request)
    {
        var clientId = Context.GetHttpContext().GetClientId();
        if (clientId <= 0)
            throw new ArgumentException($"Unable to unsubscribe from orders as clientId {clientId} is invalid");

        var groupName = CentralHubHelper.OrderGroupName(clientId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SubscribeAccountSummaryUpdates(SubscribeAccountSummaryUpdates request)
    {
        var clientId = Context.GetHttpContext().GetClientId();
        if (clientId <= 0)
            throw new ArgumentException($"Unable to subscribe to AccountSummary updates as clientId {clientId} is invalid");

        var groupName = CentralHubHelper.AccountSumaryGroupName(clientId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        await _accountSummaryAggregator.SubscribeAccountSummaryUpdates(clientId);
    }

    public async Task UnsubscribeAccountSummaryUpdates(UnsubscribeAccountSummaryUpdates request)
    {
        var clientId = Context.GetHttpContext().GetClientId();
        if (clientId <= 0)
            throw new ArgumentException($"Unable to unsubscribe from AccountSummary updates as clientId {clientId} is invalid");

        var groupName = CentralHubHelper.AccountSumaryGroupName(clientId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        await _accountSummaryAggregator.UnsubscribeAccountSummaryUpdates(clientId);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        try
        {
            var clientId = Context.GetHttpContext().GetClientId();
            await _accountSummaryAggregator.UnsubscribeAccountSummaryUpdates(clientId);
        }
        catch { return; }
    }
}
