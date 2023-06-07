namespace AlacrityCore.Infrastructure;

public abstract class MessageNexusMessage { }

public interface IMessageNexus
{
    public void FireMessage<T>(T message);
    public void SubscribeMessage<T>(Action<T> callback) where T : MessageNexusMessage;
    public void UnsubscribeMessage<T>(Action<T> callback) where T : MessageNexusMessage;
}

internal class MessageNexus : IMessageNexus
{
    // Assume that no actions will be added/removed in a Thread dangerous manner.
    private readonly Dictionary<Type, List<object>> _lookup = new();

    public void FireMessage<T>(T message)
    {
        var callbacks = _lookup.GetValueOrDefault(typeof(T));
        if (callbacks == null || callbacks.Count == 0)
            return;

        Task.Run(() =>
        {
            foreach (var callback in callbacks)
                ((Action<T>)callback)(message);
        });
    }

    public void SubscribeMessage<T>(Action<T> callback) where T : MessageNexusMessage
    {
        var callbacks = _lookup.GetValueOrDefault(typeof(T));
        if (callbacks == null)
        {
            callbacks = new();
            _lookup[typeof(T)] = callbacks;
        }

        callbacks.Add(callback);
    }

    public void UnsubscribeMessage<T>(Action<T> callback) where T : MessageNexusMessage
    {
        var callbacks = _lookup.GetValueOrDefault(typeof(T));
        if (callbacks == null)
        {
            callbacks = new();
            _lookup[typeof(T)] = callbacks;
        }

        callbacks.Remove(callback);
    }
}
