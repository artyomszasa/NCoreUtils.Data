namespace NCoreUtils.Data;

public static class RList
{
    private static readonly int[] _sizes =
    [
        4,
        8,
        16,
        32,
        48,
        64,
        80,
        96,
        128,
        192,
        256,
        1024,
        4096,
        16 * 1024
    ];

    internal static int NextCapacity(int value)
    {
        foreach (var candidate in _sizes)
        {
            if (value <= candidate)
            {
                return candidate;
            }
        }
        return value;
    }
}

public class RList<T>(int capacity) where T : struct
{
    private T[] _data = new T[RList.NextCapacity(capacity)];

    public int Capacity => _data.Length;

    public int Count { get; private set; } = 0;

    public ref T this[int index]
    {
        get
        {
            if (index < Count)
            {
                return ref _data[index];
            }
            throw new IndexOutOfRangeException();
        }
    }

    private void EnsureSize(int desired)
    {
        if (desired <= Capacity)
        {
            return;
        }
        var newSize = RList.NextCapacity(desired);
        var newData = new T[newSize];
        for (var i = 0; i < Count; ++i)
        {
            newData[i] = _data[i];
        }
        _data = newData;
    }

    private ref T AddUninitialized()
    {
        EnsureSize(Count + 1);
        ref T item = ref _data[Count];
        ++Count;
        return ref item;
    }

    public ref T Add(in T item)
    {
        ref T @ref = ref AddUninitialized();
        @ref = item;
        return ref @ref;
    }
}

