using AlacrityCore.Infrastructure;
using AlacrityCore.Services.Back.Aggregation;
using AlacrityCore.Services.Back.Exchange;
using AlacrityServer.Hubs;
using Microsoft.AspNetCore.SignalR;
using Serilog.Core;

namespace AlacrityServer.Infrastructure;

public static class MessageManager
{
    public static async Task SetupMessageManager(Logger logger, IServiceProvider serviceProvider)
    {
        var messageNexus = serviceProvider.GetRequiredService<IMessageNexus>();
        logger.Warning("Setting up MessageNexus for SignalR");
        var hubContext = serviceProvider.GetRequiredService<IHubContext<CentralHub, ICentralHubClient>>();
        messageNexus.SubscribeMessage<PriceUpdateMessage>(
            update =>
            {
                var groupName = CentralHubHelper.InstrumentGroupName(update.LatestQuote.InstrumentId);
                hubContext.Clients.Group(groupName).ReceivePriceUpdate(update.LatestQuote);
            }
        );
        messageNexus.SubscribeMessage<IndicatorUpdateMessage>(
            update =>
            {
                var groupName = CentralHubHelper.InstrumentGroupName(update.Indicator.InstrumentId);
                hubContext.Clients.Group(groupName).ReceiveIndicatorUpdate(update.Indicator);
            }
        );
        messageNexus.SubscribeMessage<OrderUpdateMessage>(
            update =>
            {
                var groupName = CentralHubHelper.OrderGroupName(update.ClientId);
                hubContext.Clients.Group(groupName).ReceiveOrderUpdate(update.Order);
            }
        );
        messageNexus.SubscribeMessage<PositionUpdateMessage>(
            update =>
            {
                var groupName = CentralHubHelper.PositionGroupName(update.ClientId);
                hubContext.Clients.Group(groupName).ReceivePositionUpdate(update.Position);
            }
        );
        messageNexus.SubscribeMessage<AccountSummaryUpdateMessage>(
            update =>
            {
                var groupName = CentralHubHelper.AccountSumaryGroupName(update.ClientId);
                hubContext.Clients.Group(groupName).ReceiveAccountSummaryUpdate(update.AccountSummary);
            }
        );
    }
}
