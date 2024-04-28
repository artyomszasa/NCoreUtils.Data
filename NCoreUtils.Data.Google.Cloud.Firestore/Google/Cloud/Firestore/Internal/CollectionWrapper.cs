using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Internal;

#if !NET6_0_OR_GREATER

public readonly struct CollectionSource<T>
{
    private readonly T[] _array;

    internal CollectionSource(T[] array)
    {
        _array = array;
    }

    public ReadOnlySpan<T> Span => _array.AsSpan();
}

#else

public readonly struct CollectionSource<T>
{
    private readonly List<T>? _list;

    private readonly T[]? _array;

    internal CollectionSource(List<T>? list, T[]? array)
    {
        _list = list;
        _array = array;
    }

    internal CollectionSource(T[] array)
        : this(default, array)
    { }

    public ReadOnlySpan<T> Span => _list is null
        ? _array is null
            ? throw new InvalidOperationException("Should never happen.")
            : _array.AsSpan()
        : CollectionsMarshal.AsSpan(_list);
}

#endif

public static class CollectionSource
{
    public static CollectionSource<T> Create<T>(IEnumerable<T> source)
    {
        if (source is T[] array)
        {

            return new(array);
        }
        if (source is List<T> list)
        {
#if NET6_0_OR_GREATER
            return new(list, default);
#else
            return new(list.ToArray());
#endif
        }
        return new(source.ToArray());
    }
}

public class AnyCollectionWrapper<T>(CollectionSource<T> source) : ICollectionWrapper
{
    protected ReadOnlySpan<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => source.Span;
    }

    public int Count => Span.Length;

    public AnyCollectionWrapper(IEnumerable<T> source)
        : this(CollectionSource.Create(source))
    { }

    public void SplitIntoChunks(int chunkSize, List<object> results)
    {
        var span = Span;
        for (var offset = 0; offset < span.Length; offset += chunkSize)
        {
            var size = Math.Min(span.Length - offset, chunkSize);
            var chunk = new T[size];
            span[offset .. (offset + size)].CopyTo(chunk.AsSpan());
            results.Add(chunk);
        }
    }
}