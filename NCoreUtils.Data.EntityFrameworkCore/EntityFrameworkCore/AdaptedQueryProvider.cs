extern alias reactive;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider;
using EFQueryableExtensions = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions;
using EFRelationalQueryableExtensions = Microsoft.EntityFrameworkCore.RelationalQueryableExtensions;
using System.Linq;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    class AdaptedQueryProvider : Linq.IAsyncQueryProvider
    {
        sealed class EnumeratorAdapter<T> : IAsyncEnumerator<T>
        {
            readonly reactive::System.Collections.Generic.IAsyncEnumerator<T> _source;

            readonly CancellationToken _cancellationToken;

            public EnumeratorAdapter(reactive::System.Collections.Generic.IAsyncEnumerator<T> source, CancellationToken cancellationToken)
            {
                _source = source;
                _cancellationToken = cancellationToken;
            }

            public T Current { get; private set; }

            public ValueTask DisposeAsync()
            {
                _source.Dispose();
                return default;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await _source.MoveNext(_cancellationToken))
                {
                    Current = _source.Current;
                    return true;
                }
                Current = default;
                return false;
            }
        }

        sealed class EnumerableAdapter<T> : IAsyncEnumerable<T>
        {
            readonly reactive::System.Collections.Generic.IAsyncEnumerable<T> _source;

            public EnumerableAdapter(reactive::System.Collections.Generic.IAsyncEnumerable<T> source)
                => _source = source;

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new EnumeratorAdapter<T>(_source.GetEnumerator(), cancellationToken);
        }

        public static AdaptedQueryProvider SharedInstance { get; } = new AdaptedQueryProvider();

        public IAsyncEnumerable<T> ExecuteEnumerableAsync<T>(Expression expression)
        {
            if (expression.TryExtractQueryable(out var sourceQueryable, typeof(EFQueryableExtensions), typeof(EFRelationalQueryableExtensions)))
            {
                var q = sourceQueryable.Provider.CreateQuery<T>(expression);
                // see: Microsoft.EntityFrameworkCore.Extensions.Internal.QueryableExtensions.AsAsyncEnumerable
                #pragma warning disable EF1001
                var enumerable = Microsoft.EntityFrameworkCore.Extensions.Internal.QueryableExtensions.AsAsyncEnumerable(q);
                #pragma warning restore EF1001
                return new EnumerableAdapter<T>(enumerable);
            }
            throw new InvalidOperationException($"Unable to extract EF queryable from {expression}.");
        }

        public Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            #pragma warning disable EF1001
            return expression.MaybeExtractQueryable(typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions), typeof(Microsoft.EntityFrameworkCore.RelationalQueryableExtensions))
                .Bind(queryable => queryable.Provider
                    .Just()
                    .As<IEFAsyncQueryProvider>()
                    .Map(provider => provider.ExecuteAsync<T>(expression, cancellationToken))
                )
                .TryGetValue(out var result)
                ? result
                : throw new InvalidOperationException();
            #pragma warning restore EF1001
        }
    }
}