namespace AlacrityCore.Utils;
public class RingBuffer<T>
{
    private readonly int _capacity;

    private int _count;
    private int _base;
    private T[] _buffer;

    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Buffer capacity must be greater than 0");
        
        _capacity = capacity;
        _buffer = new T[capacity];
    }

    public void Push(T item)
    {
        var nextIndex = RealIndexFromVirtualIndex(_count);

        _buffer[nextIndex] = item;

        if (_count < _capacity)
            _count += 1;
        else
        {
            _base += 1;
            if (_base == _capacity)
                _base = 0;
        }
    }
    
    public T Pop()
    {
        if (_count <= 0)
            throw new InvalidOperationException("Cannot pop RingBuffer as count is 0");

        var previousIndex = RealIndexFromVirtualIndex(_count - 1);        

        var item = _buffer[previousIndex];
        _buffer[previousIndex] = default;        

        _count -= 1;
        return item;
    }

    public T Peek()
    {
        if (_count == 0)
            throw new InvalidOperationException("Cannot peek as no entries in buffer");

        return _buffer[RealIndexFromVirtualIndex(Count - 1)];
    }


    public T this[int i]
    {
        get
        {
            if (i > _count - 1 || i < 0)
                throw new ArgumentException("Invalid Index");

            return _buffer[RealIndexFromVirtualIndex(i)];
        }
        set
        {
            if (i > _count - 1 || i < 0)
                throw new ArgumentException("Invalid Index");

            _buffer[RealIndexFromVirtualIndex(i)] = value;
        }
    }

    public int Count => _count;

    internal int RealIndexFromVirtualIndex(int virtualIndex)
    {
        var realIndex = _base + virtualIndex;
        if (realIndex >= _capacity)
            realIndex -= _capacity;
        else if (realIndex < 0)
            realIndex += _capacity;

        return realIndex;
    }
}
