using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.InMemory
{
    public class EnumerableQueryProviderAdapter : IAsyncQueryAdapter
    {
        private sealed class AsyncExecutor : IAsyncQueryProvider
        {
            public static AsyncExecutor Instance { get; } = new AsyncExecutor();

            public Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
            {
                if (expression.TryExtractQueryable(out var queryable))
                {
                    return Task.FromResult((T)queryable.Provider.Execute(expression)!);
                }
                throw new InvalidOperationException("Should never happen.");
            }

            public IAsyncEnumerable<T> ExecuteEnumerableAsync<T>(Expression expression)
            {
                if (expression.TryExtractQueryable(out var queryable))
                {
                    return queryable.Provider.Execute<IEnumerable<T>>(expression).ToAsyncEnumerable();
                }
                throw new InvalidOperationException("Should never happen.");
            }
        }

        public ValueTask<IAsyncQueryProvider> GetAdapterAsync(
            Func<ValueTask<IAsyncQueryProvider>> next,
            IQueryProvider source,
            CancellationToken cancellationToken)
        {
            var sourceType = source.GetType();
            if (sourceType.IsGenericType && sourceType.GetGenericTypeDefinition() == typeof(EnumerableQuery<>))
            {
                return new ValueTask<IAsyncQueryProvider>(AsyncExecutor.Instance);
            }
            return next();
        }
    }
}