using AlacrityCore.Utils;

namespace AlacrityCore.Infrastructure;
public abstract class Job<T>
{
    protected IALogger _logger;
    private bool _started;

    public abstract string JobName { get; }
    public abstract bool IsInitialized { get; }

    private object _lock = new();
    private CancellationTokenSource _cts;
    private CancellationToken _ctRaw;
    protected CancellationToken _ct => _ctRaw;

    public Job(IALogger logger)
       => (_logger) = (logger);

    /// <summary>
    /// Kick off job
    /// </summary>
    public async Task Start()
    {
        lock (_lock)
        {
            if (_started)
            {
                _logger.Error("Attempt to start {JobName} which was already started", JobName);
                throw new InvalidOperationException($"{JobName} already started");
            }
            _started = true;
        }

        _cts = new();
        _ctRaw = _cts.Token;

        await OnStart();
        var thread = new Thread(Work);
        thread.Start();
    }

    /// <summary>
    /// Wind down service, waiting for the job to stop.
    /// </summary>        
    public async Task Stop()
    {
        lock (_lock)
        {
            if (!_started || _cts == null)
            {
                _logger.Error("Attempted to stop {JobName} which was already stopped", JobName);
                throw new InvalidOperationException($"{JobName} already stopped");
            }

            _cts.Cancel();
            _cts.Dispose();
        }

        var stopTime = DateTime.UtcNow;
        await TaskUtil.AwaitPredicate(() => !IsInitialized);

        _cts = null;
        await OnStop();
    }

    protected virtual Task OnStart() { return Task.Delay(0); }
    protected virtual Task OnStop() { return Task.Delay(0); }

    protected abstract void Work();
}
