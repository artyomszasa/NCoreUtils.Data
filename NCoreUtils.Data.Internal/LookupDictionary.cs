#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#else
using System.Collections.Immutable;
#endif

namespace NCoreUtils.Data.Internal;

/// <summary>
/// A <c>Dictionary</c> implementation optimized for value retrieval. Inserting value is relatively expensive!
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TValue">Value type.</typeparam>
internal sealed class LookupDictionary<TKey, TValue>
    where TKey : notnull
{
#if NET8_0_OR_GREATER
    private static FrozenDictionary<TKey, TValue> EmptyData() => FrozenDictionary<TKey, TValue>.Empty;

    private static FrozenDictionary<TKey, TValue> WithItem(FrozenDictionary<TKey, TValue> source, TKey key, TValue value)
        => source.Append(new(key, value)).ToFrozenDictionary();

    private FrozenDictionary<TKey, TValue> _data = EmptyData();
#else
    private static ImmutableDictionary<TKey, TValue> EmptyData() => ImmutableDictionary<TKey, TValue>.Empty;

    private static ImmutableDictionary<TKey, TValue> WithItem(ImmutableDictionary<TKey, TValue> source, TKey key, TValue value)
        => source.Add(key, value);

    private ImmutableDictionary<TKey, TValue> _data = EmptyData();
#endif

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
                // var @new = new Dictionary<TKey, TValue>(current) { { key, value } }.ToFrozenDictionary();
                var @new = WithItem(current, key, value);
                success = ReferenceEquals(current, Interlocked.CompareExchange(ref _data, @new, current));
            }
            while (!success);


        }
        return value;
    }

    public TValue[] Purge()
        => [.. Interlocked.Exchange(ref _data, EmptyData()).Values];
}