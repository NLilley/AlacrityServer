using AlacrityCore.Enums;
using AlacrityCore.Infrastructure;
using AlacrityCore.Models.Back;
using AlacrityCore.Models.DTOs;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back.Exchange;
using System.Collections.Concurrent;

namespace AlacrityCore.Services.Back.Aggregation;
public interface IAccountSummaryAggregator
{
    public Task SubscribeAccountSummaryUpdates(int clientId);
    public Task UnsubscribeAccountSummaryUpdates(int clientId);
}

internal class AccountSummaryAggregator : Job<AccountSummaryAggregator>, IAccountSummaryAggregator
{
    public override string JobName => nameof(AccountSummaryAggregator);
    private bool _isInitialized = false;

    private readonly IMessageNexus _messageNexus;
    private readonly IPositionsQuery _positionsQuery;
    private readonly IWebMessageQuery _webMessageQuery;
    private readonly IExchange _exchange;    
    public AccountSummaryAggregator(
        IALogger logger,
        IPositionsQuery positionsQuery,
        IWebMessageQuery webMessageQuery,
        IExchange exchange,
        IMessageNexus messageNexus
    ) : base(logger)
    {
        _positionsQuery = positionsQuery;
        _webMessageQuery = webMessageQuery;
        _exchange = exchange;
        _messageNexus = messageNexus;
    }

    public override bool IsInitialized => _isInitialized;

    private ConcurrentDictionary<int, AccountSummaryWrapper> _subscribedClients = new();
    private ConcurrentBag<long> _updatedPrices = new();

    protected override async Task OnStart()
    {
        await base.OnStart();
        SubscribeMessages();
        _isInitialized = true;
    }

    protected override async Task OnStop()
    {
        await base.OnStop();
        _isInitialized = false;
    }

    private void SubscribeMessages()
    {
        _messageNexus.SubscribeMessage<PositionUpdateMessage>(p =>
        {
            if (!_subscribedClients.TryGetValue(p.ClientId, out var accountSummaryWrapper))
                return;

            accountSummaryWrapper.IsDirty = true;
        });

        _messageNexus.SubscribeMessage<OrderUpdateMessage>(o =>
        {
            if (!_subscribedClients.TryGetValue(o.ClientId, out var accountSummaryWrapper))
                return;

            accountSummaryWrapper.IsDirty = true;
        });

        _messageNexus.SubscribeMessage<PriceUpdateMessage>(p =>
        {
            _updatedPrices.Add(p.LatestQuote.InstrumentId);
        });

        _messageNexus.SubscribeMessage<TradeExecutedMessage>(t =>
        {
            if (!_subscribedClients.TryGetValue(t.ClientId, out var accountSummaryWrapper))
                return;

            accountSummaryWrapper.IsDirty = true;            
        });
    }

    protected override async void Work()
    {
        while (!_ct.IsCancellationRequested)
        {
            foreach (var (clientId, accountSummaryWrapper) in _subscribedClients)
            {
                if (!accountSummaryWrapper.IsDirty && accountSummaryWrapper.InstrumentIds.Intersect(_updatedPrices).FirstOrDefault() == 0)
                    continue;

                var currentAccountSummaryWrapper = await GetAccountSummaryWrapper(clientId);
                if (currentAccountSummaryWrapper.AccountSummary == accountSummaryWrapper.AccountSummary)
                    continue;

                _subscribedClients.TryUpdate(
                    clientId,
                    currentAccountSummaryWrapper,
                    accountSummaryWrapper
                );

                _messageNexus.FireMessage(new AccountSummaryUpdateMessage
                {
                    ClientId = clientId,
                    AccountSummary = currentAccountSummaryWrapper.AccountSummary
                });
            }

            _updatedPrices.Clear();

            try { await Task.Delay(TimeSpan.FromMilliseconds(500)); }
            catch { }
        }
    }

    public async Task SubscribeAccountSummaryUpdates(int clientId)
    {
        _logger.Info("Starting aggregation of AccountSummaries for ClientId {clientId}", clientId);

        var accountSummaryWrapper = await GetAccountSummaryWrapper(clientId);

        _subscribedClients.AddOrUpdate(clientId, accountSummaryWrapper, (_, _) => accountSummaryWrapper);

        _messageNexus.FireMessage(new AccountSummaryUpdateMessage()
        {
            ClientId = clientId,
            AccountSummary = accountSummaryWrapper.AccountSummary
        });
    }

    public async Task UnsubscribeAccountSummaryUpdates(int clientId)
    {
        _logger.Info("Stopping aggregation of AcccountSummaries for ClientId {clientId}", clientId);
        _subscribedClients.Remove(clientId, out var _);
    }

    private async Task<AccountSummaryWrapper> GetAccountSummaryWrapper(int clientId)
    {
        var positions = await _positionsQuery.GetPositions(clientId);

        var cashBalance = 0m;
        var accountValue = 0m;
        var profitLoss = 0m;
        foreach (var position in positions)
        {
            if (position.InstrumentId == (long)SpecialInstruments.Cash)
            {
                cashBalance += position.Quantity;
                accountValue += cashBalance;
                continue;
            }

            var instrumentPrice = await _exchange.GetQuote(position.InstrumentId);
            var mid = (instrumentPrice.Bid + instrumentPrice.Ask) / 2;
            accountValue += (mid * position.Quantity) ?? 0;
            profitLoss += ((mid - position.AveragePrice) * position.Quantity) ?? 0;
        }

        var messageThreads = await _webMessageQuery.GetWebMessageThreads(clientId);
        var unread = messageThreads.Sum(t => t.WebMessages.Sum(m => m.Read ? 0 : 1));

        return new()
        {
            ClientId = clientId,
            AccountSummary = new()
            {
                AccountValue = accountValue,
                CashBalance = cashBalance,
                ProfitLoss = profitLoss,
                UnreadMessages = unread
            },
            InstrumentIds = positions.Select(p => p.InstrumentId).Where(p => p != (long)SpecialInstruments.Cash).ToList()
        };
    }

    private record AccountSummaryWrapper
    {
        public int ClientId { get; set; }
        public bool IsDirty { get; set; }
        public AccountSummaryDto AccountSummary { get; set; }
        public List<long> InstrumentIds { get; set; }
    }
}

public class AccountSummaryUpdateMessage : MessageNexusMessage
{
    public int ClientId { get; set; }
    public AccountSummaryDto AccountSummary { get; set; }
}
