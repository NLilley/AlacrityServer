using AlacrityCore.Database;
using AlacrityCore.Infrastructure;
using AlacrityCore.Queries;
using AlacrityCore.Services.Back;
using AlacrityCore.Services.Back.Aggregation;
using AlacrityCore.Services.Back.Communication;
using AlacrityCore.Services.Back.Exchange;
using AlacrityCore.Services.Front;
using AlacrityCore.Utils;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AlacrityServer.Infrastructure;

public static class StartUp
{
    // Sneak dependencies into Microsoft DI    
    private static IServiceScope _serviceScope;
    private static Exchange _exchange;
    private static MarketParticipantManager _marketParticipantManager;
    private static PriceAggregator _priceAggregator;
    private static AccountSummaryAggregator _accountSummaryAggregator;
    private static Messenger _messenger;

    public static void SetUpCoreService(ILogger logger, IALogger backServiceLogger, IServiceCollection services)
    {
        var executableDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        var databasePath = Path.Join(executableDir, "DataBase/Data/alacrity.db");
        var databaseConnectionString = DatabaseSetup.GetConnectionStringFromPath(databasePath);

        logger.Warning($"Setting up database in path: {databasePath}");
        DatabaseSetup.Setup(databasePath);

        services.AddSingleton<IALogger>(backServiceLogger);
        var messageNexus = new MessageNexus();
        services.AddSingleton<IMessageNexus>(messageNexus);

        #region Queries
        services.AddTransient(provider => new SqliteConnection(databaseConnectionString));

        services.AddTransient<IAuthenticationQuery, AuthenticationQuery>();
        services.AddTransient<IClientsQuery, ClientsQuery>();
        services.AddTransient<IInstrumentsQuery, InstrumentsQuery>();
        services.AddTransient<ILedgerQuery, LedgerQuery>();
        services.AddTransient<IOrdersQuery, OrdersQuery>();
        services.AddTransient<IPositionsQuery, PositionsQuery>();
        services.AddTransient<IPriceHistoryQuery, PriceHistoryQuery>();
        services.AddTransient<ISearchQuery, SearchQuery>();
        services.AddTransient<IStatementsQuery, StatementsQuery>();
        services.AddTransient<ITradesQuery, TradesQuery>();
        services.AddTransient<IWatchlistQuery, WatchlistQuery>();
        services.AddTransient<IWebMessageQuery, WebMessageQuery>();
        services.AddTransient<IWebMessageUserQuery, WebMessageUserQuery>();
        #endregion

        #region Front Services
        services.AddTransient<IAuthenticationFrontService, AuthenticationFrontService>();
        services.AddTransient<IClientsFrontService, ClientsFrontService>();
        services.AddTransient<IInstrumentFrontService, InstrumentFrontService>();
        services.AddTransient<IOrdersFrontService, OrdersFrontService>();
        services.AddTransient<IPositionsFrontService, PositionsFrontService>();
        services.AddTransient<IPriceHistoryFrontService, PriceHistoryFrontService>();
        services.AddTransient<ISearchFrontService, SearchFrontService>();
        services.AddTransient<IStatementsFrontService, StatementsFrontService>();
        services.AddTransient<ITradesFrontService, TradesFrontService>();
        services.AddTransient<ITransactionFrontService, TransactionFrontService>();
        services.AddTransient<IWatchlistsFrontService, WatchlistsFrontService>();
        services.AddTransient<IWebMessageFrontService, WebMessagesFrontService>();
        #endregion

        #region Back Services
        services.AddTransient<IClientsBackService, ClientsBackService>();
        services.AddTransient<IInstrumentBackService, InstrumentBackService>();
        services.AddTransient<ILedgerBackService, LedgerBackService>();
        services.AddTransient<IPositionsBackService, PositionsBackService>();
        services.AddTransient<IPriceHistoryBackService, PriceHistoryBackService>();
        #endregion

        #region Special Services
        services.AddSingleton<IExchange>(provider => _exchange);
        services.AddSingleton<IAccountSummaryAggregator>(provider => _accountSummaryAggregator);
        services.AddSingleton<ITransactionLock>(new TransactionLock());
        #endregion
    }

    public static async Task InitStaticDependencies(ILogger serverLogger, IALogger exchangeLogger, IServiceProvider serviceProvider)
    {
        _serviceScope = serviceProvider.CreateScope();
        var provider = _serviceScope.ServiceProvider;

        var messageNexus = provider.GetService<IMessageNexus>();
        serverLogger.Warning("Creating Exchange");
        _exchange = new Exchange(
            exchangeLogger,
            provider.GetService<IOrdersQuery>(),
            provider.GetService<IInstrumentsQuery>(),
            provider.GetService<ITradesQuery>(),
            provider.GetService<IPositionsQuery>(),
            provider.GetService<ILedgerQuery>(),
            messageNexus,
            provider.GetService<ITransactionLock>()
        );
        await _exchange.Start();
        var exchangeWait = await TaskUtil.AwaitPredicate(() => _exchange.IsInitialized);
        serverLogger.Warning("Exchange created and intialized in {wait}", exchangeWait);

        serverLogger.Warning("Creating MarketParticipantManager");
        _marketParticipantManager = new MarketParticipantManager(
            exchangeLogger,
            provider.GetService<IInstrumentsQuery>(),
            provider.GetService<IPriceHistoryQuery>(),
            _exchange
        );

        await _marketParticipantManager.Start();
        var marketParticipantWait = await TaskUtil.AwaitPredicate(
            () => _marketParticipantManager.IsInitialized,
            TimeSpan.FromSeconds(30)
        );
        serverLogger.Warning("MarketParticipantManager created and initialized in {wait}", marketParticipantWait);

        serverLogger.Warning("Creating PriceAggregator");
        _priceAggregator = new PriceAggregator(
            exchangeLogger,
            provider.GetService<IPriceHistoryQuery>(),
            provider.GetService<IInstrumentsQuery>(),
            _exchange,
            messageNexus
        );
        await _priceAggregator.Start();
        var priceAggregatorWait = await TaskUtil.AwaitPredicate(() => _priceAggregator.IsInitialized);
        serverLogger.Warning("PriceAggregator created and initialized in {wait}", priceAggregatorWait);

        serverLogger.Warning("Creating AccountSummaryAggregator");
        _accountSummaryAggregator = new AccountSummaryAggregator(
            exchangeLogger,
            provider.GetService<IPositionsQuery>(),
            provider.GetService<IWebMessageQuery>(),
            _exchange,
            messageNexus                        
        );
        await _accountSummaryAggregator.Start();
        serverLogger.Warning("AccountSummaryAggregator created and initialized");

        _messenger = new Messenger(
            serverLogger,
            messageNexus,
            provider.GetService<IClientsQuery>(),
            provider.GetService<IInstrumentsQuery>(),
            provider.GetService<IWebMessageQuery>()
        );
        _messenger.SubscribeMessages();
    }

    public static void Stop()
    {
        _priceAggregator?.Stop();
        _marketParticipantManager?.Stop();
        _exchange?.Stop();
        _accountSummaryAggregator?.Stop();
    }
}
