using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    public static class QueryableExtensions
    {
        #region Helper methods to obtain MethodInfo in a safe way
        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 unused1)
            => f.GetMethodInfo();
        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 unused1, T2 unused2)
            => f.GetMethodInfo();
        private static MethodInfo GetMethodInfo<T1, T2, T3, T4>(Func<T1, T2, T3, T4> f, T1 unused1, T2 unused2, T3 unused3)
            => f.GetMethodInfo();
        private static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4)
            => f.GetMethodInfo();
        private static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5)
            => f.GetMethodInfo();
        private static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7> f, T1 unused1, T2 unused2, T3 unused3, T4 unused4, T5 unused5, T6 unused6)
            => f.GetMethodInfo();
        #endregion

        static async Task<IAsyncQueryProvider> GetAsync(this IQueryProvider provider, CancellationToken cancellationToken)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            if (provider is IAsyncQueryProvider asyncProvider)
            {
                return asyncProvider;
            }
            var adaptedProvider = await AsyncQueryAdapters.AdaptAsync(provider, cancellationToken);
            if (null != adaptedProvider)
            {
                return adaptedProvider;
            }
            throw new InvalidOperationException($"{provider.GetType().FullName} cannot be adapted.");
        }

        public static Task<bool> AllAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            var negPredicate = Expression.Lambda<Func<T, bool>>(Expression.Not(predicate.Body), predicate.Parameters);
            return source.AnyAsync(negPredicate, cancellationToken);
        }

        public static async Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<bool>(Expression.Call(
                null,
                GetMethodInfo(Queryable.Any, source),
                Expression.Constant(source)
            ), cancellationToken);
        }

        public static Task<bool> AnyAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return source.Where(predicate).AnyAsync(cancellationToken);
        }

        public static async Task<bool> ContainsAsync<T>(this IQueryable<T> source, T item, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<bool>(Expression.Call(
                null,
                GetMethodInfo(Queryable.Contains, source, item),
                Expression.Constant(source),
                Expression.Constant(item)
            ), cancellationToken);
        }

        public static async Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<int>(Expression.Call(
                null,
                GetMethodInfo(Queryable.Count, source),
                Expression.Constant(source)
            ), cancellationToken);
        }

        public static Task<int> CountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return source.Where(predicate).CountAsync(cancellationToken);
        }
        public static async Task<T> ElementAtAsync<T>(this IQueryable<T> source, int index, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<T>(Expression.Call(
                null,
                GetMethodInfo(Queryable.ElementAt, source, index),
                Expression.Constant(source),
                Expression.Constant(index)
            ), cancellationToken);
        }

        public static async Task<T> ElementAtOrDefaultAsync<T>(this IQueryable<T> source, int index, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<T>(Expression.Call(
                null,
                GetMethodInfo(Queryable.ElementAtOrDefault, source, index),
                Expression.Constant(source),
                Expression.Constant(index)
            ), cancellationToken);
        }

        public static async Task<T> FirstAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<T>(Expression.Call(
                null,
                GetMethodInfo(Queryable.First, source),
                Expression.Constant(source)
            ), cancellationToken);
        }

        public static async Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<T>(Expression.Call(
                null,
                GetMethodInfo(Queryable.FirstOrDefault, source),
                Expression.Constant(source)
            ), cancellationToken);
        }

        public static Task<T> FirstAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return source.Where(predicate).FirstAsync(cancellationToken);
        }

        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return source.Where(predicate).FirstOrDefaultAsync(cancellationToken);
        }

        public static async Task<long> LongCountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<long>(Expression.Call(
                null,
                GetMethodInfo(Queryable.LongCount, source),
                Expression.Constant(source)
            ), cancellationToken);
        }

        public static Task<long> LongCountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return source.Where(predicate).LongCountAsync(cancellationToken);
        }

        public static async Task<T> SingleAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<T>(Expression.Call(
                null,
                GetMethodInfo(Queryable.Single, source),
                Expression.Constant(source)
            ), cancellationToken);
        }

        public static async Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            return await asyncProvider.ExecuteAsync<T>(Expression.Call(
                null,
                GetMethodInfo(Queryable.SingleOrDefault, source),
                Expression.Constant(source)
            ), cancellationToken);
        }

        public static Task<T> SingleAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return source.Where(predicate).SingleAsync(cancellationToken);
        }
        public static Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }
            return source.Where(predicate).SingleOrDefaultAsync(cancellationToken);
        }
        public static async Task<T[]> ToArrayAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            var list = await source.ToListAsync(cancellationToken);
            return list.ToArray();
        }
        public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            var result = new List<T>();
            var asyncEnumerable = asyncProvider.ExecuteAsync<T>(source.Expression);
            using (var enumerator = asyncEnumerable.GetEnumerator())
            {
                while (await enumerator.MoveNext(cancellationToken))
                {
                    result.Add(enumerator.Current);
                }
            }
            return result;
        }

        public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement>(
            this IQueryable<TElement> source,
            Func<TElement, TKey> keySelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            var result = new Dictionary<TKey, TElement>(comparer ?? EqualityComparer<TKey>.Default);
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            var asyncEnumerable = asyncProvider.ExecuteAsync<TElement>(source.Expression);
            using (var enumerator = asyncEnumerable.GetEnumerator())
            {
                while (await enumerator.MoveNext(cancellationToken))
                {
                    var current = enumerator.Current;
                    result.Add(keySelector(current), current);
                }
            }
            return result;
        }

        public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement>(
            this IQueryable<TElement> source,
            Func<TElement, TKey> keySelector,
            CancellationToken cancellationToken)
            => source.ToDictionaryAsync(keySelector, null, cancellationToken);

        public static async Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement, TSource>(
            this IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> valueSelector,
            IEqualityComparer<TKey> comparer,
            CancellationToken cancellationToken)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }
            if (valueSelector == null)
            {
                throw new ArgumentNullException(nameof(valueSelector));
            }
            var result = new Dictionary<TKey, TElement>(comparer ?? EqualityComparer<TKey>.Default);
            var asyncProvider = await source.Provider.GetAsync(cancellationToken);
            var asyncEnumerable = asyncProvider.ExecuteAsync<TSource>(source.Expression);
            using (var enumerator = asyncEnumerable.GetEnumerator())
            {
                while (await enumerator.MoveNext(cancellationToken))
                {
                    var current = enumerator.Current;
                    result.Add(keySelector(current), valueSelector(current));
                }
            }
            return result;
        }

        public static Task<Dictionary<TKey, TElement>> ToDictionaryAsync<TKey, TElement, TSource>(
            this IQueryable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> valueSelector,
            CancellationToken cancellationToken)
            => source.ToDictionaryAsync(keySelector, valueSelector, null, cancellationToken);

        public static IAsyncEnumerable<T> ExecuteAsync<T>(this IQueryable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return new DelayedAsyncEnumerable<T>(async cancellationToken =>
            {
                var asyncProvider = await source.Provider.GetAsync(cancellationToken);
                return asyncProvider.ExecuteAsync<T>(source.Expression);
            });
        }
    }
}