using System.Collections.Concurrent;

namespace AlacrityCore.Infrastructure;

internal interface ITransactionLock
{
    public object GetLocker(int clientId);
}

// Not the most elegant solution for providing synchronisation - but it will surfice for now
internal class TransactionLock : ITransactionLock
{
    // ClientId -> lock object
    private static ConcurrentDictionary<int, object> _locks = new();
    public object GetLocker(int clientId) => _locks.GetOrAdd(clientId, i => new());
}
