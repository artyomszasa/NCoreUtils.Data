using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace NCoreUtils.Data;

internal static class HashHelper
{
    private const int HashPrime = 101;

    private const int MaxPrimeArrayLength = 0x7FEFFFFD;

    public static readonly int[] primes = [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    ];

    public static bool IsPrime(int candidate)
    {
        if ((candidate & 1) != 0)
        {
            int limit = (int)Math.Sqrt(candidate);
            for (int divisor = 3; divisor <= limit; divisor += 2)
            {
                if ((candidate % divisor) == 0)
                {
                    return false;
                }
            }
            return true;
        }
        return candidate == 2;
    }

    public static int GetPrime(int min)
    {
        if (min < 0)
        {
            throw new ArgumentException("{0} must be non-negative", nameof(min));
        }

        for (int i = 0; i < primes.Length; i++)
        {
            int prime = primes[i];
            if (prime >= min)
            {
                return prime;
            }
        }

        for (int i = min | 1; i < int.MaxValue; i += 2)
        {
            if (IsPrime(i) && ((i - 1) % HashPrime != 0))
            {
                return i;
            }
        }
        return min;
    }

    public static int GetMinPrime()
    {
        return primes[0];
    }

    public static int ExpandPrime(int oldSize)
    {
        int newSize = 2 * oldSize;

        // Allow the hashtables to grow to maximum possible size (~2G elements) before encoutering capacity overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
        {
            return MaxPrimeArrayLength;
        }

        return GetPrime(newSize);
    }
}

internal static class RDictionaryArrayExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Fill<T>(this T[] array, T value = default!)
    {

        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }
        for (int i = 0; i < array.Length; ++i)
        {
            array[i] = value;
        }
    }
}

internal class RDictionary
{
    private struct Entry
    {
        /// <summary>
        /// Lower 31 bits of the hash code.
        /// </summary>
        public int HashCode;

        /// <summary>
        /// Index of the next entry.
        /// </summary>
        public int Next;

        /// <summary>
        /// The value of the entry.
        /// </summary>
        public TypeWrapper Value;
    }

    private int[]? buckets;

    private Entry[]? entries;

    private int version;

    private int freeIndex;

    private int freeCount;

    private int count;

    public RDictionary(int capacity = 0)
    {
        if (capacity < 0 || int.MaxValue < capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }
        if (capacity > 0)
        {
            Initialize(capacity);
        }
    }

    [MemberNotNull(nameof(buckets))]
    [MemberNotNull(nameof(entries))]
    private void Initialize(int capacity)
    {
        int size = HashHelper.GetPrime(capacity);
        buckets = new int[size];
        buckets.Fill(-1);
        entries = new Entry[size];
        freeIndex = -1;
    }


    [MemberNotNull(nameof(entries))]
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    private void MarkEntriesAsInitialized() { /* noop */ }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [MemberNotNull(nameof(buckets))]
    [MemberNotNull(nameof(entries))]
    private void EnsureInitialized()
    {
        if (buckets is null)
        {
            Initialize(0);
        }
        MarkEntriesAsInitialized();
    }

    private void Resize(int newSize, bool forceNewHashCodes)
    {
        if (!(newSize > entries!.Length))
        {
            throw new ArgumentException(nameof(newSize) + " must be greater than the size of the entries array.");
        }
        int[] newBuckets = new int[newSize];
        for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
        Entry[] newEntries = new Entry[newSize];
        Array.Copy(entries, 0, newEntries, 0, count);
        if (forceNewHashCodes)
        {
            for (int i = 0; i < count; i++)
            {
                if (newEntries[i].HashCode != -1)
                {
                    newEntries[i].HashCode = GetKeyHasCode(newEntries[i].Value.Symbol);
                }
            }
        }
        for (int i = 0; i < count; i++)
        {
            if (newEntries[i].HashCode >= 0)
            {
                int bucket = newEntries[i].HashCode % newSize;
                newEntries[i].Next = newBuckets[bucket];
                newBuckets[bucket] = i;
            }
        }
        buckets = newBuckets;
        entries = newEntries;
    }

    private void Resize() => Resize(HashHelper.ExpandPrime(count), false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetKeyHasCode(ITypeSymbol key)
        => SymbolEqualityComparer.Default.GetHashCode(key) & 0x7FFFFFFF;

    public ref TypeWrapper GetOrAdd(ITypeSymbol key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        EnsureInitialized();
        var hashCode = GetKeyHasCode(key);
        var targetBucket = hashCode % buckets.Length;
        ref int targetBucketStart = ref buckets[targetBucket];
        var i = targetBucketStart;
        while (i != -1)
        {
            ref Entry entry = ref entries[i];
            if (entry.HashCode == hashCode && SymbolEqualityComparer.Default.Equals(entry.Value.Symbol, key))
            {
                return ref entry.Value;
            }
            i = entry.Next;
        }
        int index;
        if (freeCount > 0)
        {
            index = freeIndex;
            freeIndex = entries[index].Next;
            --freeCount;
        }
        else
        {
            if (count == entries.Length)
            {
                Resize();
                targetBucket = hashCode % buckets.Length;
            }
            index = count;
            ++count;
        }
        {
            ref Entry entry = ref entries[index];
            entry.HashCode = hashCode;
            entry.Next = buckets[targetBucket];
            entry.Value = new(key);
            targetBucketStart = index;
            unchecked
            {
                ++version;
            }
            return ref entry.Value;
        }
    }

    private static TypeWrapper Dummy = default;

    public ref struct Enumerator(RDictionary self, int count)
    {
        private readonly Entry[] entries = self.entries ?? [];

        private int index = -1;

        public readonly ref TypeWrapper Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index == -1 || index >= count)
                {
                    return ref Dummy;
                }
                ref Entry entry = ref entries[index];
                return ref entry.Value;
            }
        }

        public bool MoveNext()
        {
            if (++index >= count)
            {
                --index;
                return false;
            }
            return true;
        }
    }

    public Enumerator GetEnumerator() => new(this, count);
}