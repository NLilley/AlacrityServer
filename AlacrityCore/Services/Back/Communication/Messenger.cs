using AlacrityCore.Enums;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;
using Serilog;

namespace AlacrityCore.Services.Back.Communication;
internal class Messenger
{
    private readonly ILogger _logger;
    private readonly IMessageNexus _messageNexus;
    private readonly IClientsQuery _clientQuery;
    private readonly IInstrumentsQuery _instrumentQuery;
    private readonly IWebMessageQuery _webMessageQuery;
    public Messenger(
        ILogger logger,
        IMessageNexus messageNexus,
        IClientsQuery clientsQuery,
        IInstrumentsQuery instrumentQuery,
        IWebMessageQuery webMessageQuery
    )
    {
        _messageNexus = messageNexus;
        _clientQuery = clientsQuery;
        _instrumentQuery = instrumentQuery;
        _webMessageQuery = webMessageQuery;
    }

    public void SubscribeMessages()
    {
        _messageNexus.SubscribeMessage<TradeExecutedMessage>(async t =>
        {
            try
            {
                await Task.Run(async () =>
                {
                    await Task.Delay(500);
                    await SendTradeNotificationEmail(t.ClientId, t.Trade);
                });
            }
            catch (Exception ex) { 
            }
        });
    }

    private async Task SendTradeNotificationEmail(int clientId, TradeDto trade)
    {
        var client = await _clientQuery.GetClient(clientId);
        var instrument = await _instrumentQuery.GetInstrument(trade.InstrumentId);
        var title = $"Trade Executed: {trade.TradeDirection.ToString().ToUpper()} {trade.Quantity} {instrument.Name} @ {trade.Price}";
        var message = @$"Hello {client.FirstName},

We have just executed the following trade on your behalf.

Instrument: ""{instrument.DisplayName}""
Time: {trade.TradeDate}
Direction: {trade.TradeDirection.ToString().ToUpper()}
Quantity: {trade.Quantity}
Price: {trade.Price}
OrderId: {trade.OrderId}
TradeId: {trade.TradeId}

All the best,

- Alacrity Trading Team
";

        var webMessageUserId = await _webMessageQuery.GetWebMessageUserId(clientId);
        if (webMessageUserId == null)
            return;

        await _webMessageQuery.AddMessage(            
            rootMessageId: null,
            title: title,
            message: message,
            messageKind: WebMessageKind.TradeConfirmation,
            ownerId: webMessageUserId.Value,
            toId: webMessageUserId.Value,
            fromId: (long)SpecialWebMessageUsers.AlacrityTradingTeam,
            finalized: true
        );
    }
}
