using System;
using System.Collections.Generic;
using System.Linq;

namespace NCoreUtils.Linq
{
    public static class EnumerableExtensions
    {
        public static bool TryFirst<T>(this IEnumerable<T> source, out T value, Func<T, bool> predicate)
        {
            if (null == predicate)
            {
                return source.TryFirst(out value);
            }
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.Where(predicate).TryFirst(out value);
        }

        public static bool TryFirst<T>(this IEnumerable<T> source, out T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            using (var enumerator = source.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    value = enumerator.Current;
                    return true;
                }
            }
            value = default(T);
            return false;
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            var cmp = comparer ?? Comparer<TKey>.Default;
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence is empty.");
                }
                var max = enumerator.Current;
                var maxValue = selector(max);
                while (enumerator.MoveNext())
                {
                    var curr = enumerator.Current;
                    var currValue = selector(curr);
                    if (comparer.Compare(maxValue, currValue) < 0)
                    {
                        max = curr;
                        maxValue = currValue;
                    }
                }
                return max;
            }
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }
            var cmp = comparer ?? Comparer<TKey>.Default;
            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence is empty.");
                }
                var max = enumerator.Current;
                var maxValue = selector(max);
                while (enumerator.MoveNext())
                {
                    var curr = enumerator.Current;
                    var currValue = selector(curr);
                    if (comparer.Compare(maxValue, currValue) > 0)
                    {
                        max = curr;
                        maxValue = currValue;
                    }
                }
                return max;
            }
        }
    }
}