using System.Collections.Frozen;

namespace NCoreUtils.Data.Internal;

/// <summary>
/// A <c>Dictionary</c> implementation optimized for value retrieval. Inserting value is relatively expensive!
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
internal sealed class LookupDictionary<TKey, TValue>
    where TKey : notnull
{
    private FrozenDictionary<TKey, TValue> _data = FrozenDictionary<TKey, TValue>.Empty;

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
    {
        if (!_data.TryGetValue(key, out var value))
        {
            value = factory(key);
            bool success;
            do
            {
                var current = _data;
                Interlocked.MemoryBarrier();
                if (current.TryGetValue(key, out var v))
                {
                    // meanwhile has been added on another thread...
                    return v;
                }
                var @new = new Dictionary<TKey, TValue>(current) { { key, value } }.ToFrozenDictionary();
                success = ReferenceEquals(current, Interlocked.CompareExchange(ref _data, @new, current));
            }
            while (!success);


        }
        return value;
    }

    /*
    public TValue GetOrAdd<TArg>(TKey key, TArg arg, Func<TKey, TArg, TValue> factory)
    {
        if (!_data.TryGetValue(key, out var value))
        {
            value = factory(key, arg);
            bool success;
            do
            {
                var current = _data;
                Interlocked.MemoryBarrier();
                if (current.TryGetValue(key, out var v))
                {
                    // meanwhile has been added on another thread...
                    return v;
                }
                var @new = new Dictionary<TKey, TValue>(current) { { key, value } }.ToFrozenDictionary();
                success = ReferenceEquals(current, Interlocked.CompareExchange(ref _data, @new, current));
            }
            while (!success);


        }
        return value;
    }
    */

    public TValue[] Purge()
        => [.. Interlocked.Exchange(ref _data, FrozenDictionary<TKey, TValue>.Empty).Values];
}