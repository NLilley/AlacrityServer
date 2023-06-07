namespace AlacrityCore.Utils;
public static class TaskUtil
{
    public static async Task<TimeSpan> AwaitPredicate(Func<bool> p, TimeSpan? maxWait = null)
    {
        maxWait ??= TimeSpan.FromSeconds(5);
        var started = DateTime.UtcNow;
        while (!p())
        {
            await Task.Delay(50);
            var waited = DateTime.UtcNow - started;
            if (waited > maxWait)
                return waited;
        }

        return DateTime.UtcNow - started;
    }
}
