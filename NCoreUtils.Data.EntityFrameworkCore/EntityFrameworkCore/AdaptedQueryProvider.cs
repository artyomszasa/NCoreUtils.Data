using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    class AdaptedQueryProvider : Linq.IAsyncQueryProvider
    {
        public static AdaptedQueryProvider SharedInstance { get; } = new AdaptedQueryProvider();

        public IAsyncEnumerable<T> ExecuteAsync<T>(Expression expression)
        {
            return expression.MaybeExtractQueryable(typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions))
                .Bind(queryable => queryable.Provider
                        .Just()
                        .As<IEFAsyncQueryProvider>()
                        .Map(provider => provider.ExecuteAsync<T>(expression))
                )
                .TryGetValue(out var result)
                ? result
                : throw new InvalidOperationException();
        }

        public Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            return expression.MaybeExtractQueryable(typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions))
                .Bind(queryable => queryable.Provider
                    .Just()
                    .As<IEFAsyncQueryProvider>()
                    .Map(provider => provider.ExecuteAsync<T>(expression, cancellationToken))
                )
                .TryGetValue(out var result)
                ? result
                : throw new InvalidOperationException();
        }
    }
}