using AlacrityCore.Infrastructure;

namespace AlacrityCore.Services.Back.Util;
internal class CleanUpJob : Job<CleanUpJob>
{
    private bool _isInitialized = false;

    public override string JobName => nameof(CleanUpJob);
    public override bool IsInitialized => _isInitialized;

    private readonly IALogger _logger;
    private readonly IPriceHistoryQuery _priceHistoryQuery;
    public CleanUpJob(IALogger logger, IPriceHistoryQuery priceHistoryQuery)
        : base(logger) => (_logger, _priceHistoryQuery) = (logger, priceHistoryQuery);

    protected override async Task OnStart()
    {
        await base.OnStart();
        _isInitialized = true;
    }

    protected override async Task OnStop()
    {
        await base.OnStop();
        _isInitialized = false;
    }

    protected override async void Work()
    {
        while(!_ct.IsCancellationRequested)
        {
            try { await Task.Delay(TimeSpan.FromMinutes(5), _ct); }
            catch { }

            var olderThan = DateTime.UtcNow.Subtract(TimeSpan.FromHours(1));
            await _priceHistoryQuery.DeleteOldCandles(olderThan);
        }
    }
}
