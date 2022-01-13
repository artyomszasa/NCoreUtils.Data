// extern alias reactive;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EFQueryableExtensions = Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions;
using EFRelationalQueryableExtensions = Microsoft.EntityFrameworkCore.RelationalQueryableExtensions;
#if NET6_0_OR_GREATER
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider;
#else
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider;
#endif

#pragma warning disable EF1001

namespace NCoreUtils.Data.EntityFrameworkCore
{
    class AdaptedQueryProvider : Linq.IAsyncQueryProvider
    {
        public static AdaptedQueryProvider SharedInstance { get; } = new AdaptedQueryProvider();

        public IAsyncEnumerable<T> ExecuteEnumerableAsync<T>(Expression expression)
        {
            // Handle EF Core 6.0
#if NET6_0_OR_GREATER
            if (expression.TryExtractQueryProvider(out var queryProvider, typeof(EFQueryableExtensions), typeof(EFRelationalQueryableExtensions)))
            {
                return queryProvider.ExecuteAsync<IAsyncEnumerable<T>>(expression);
            }
#else
            // Handle EF Core 3.1
            if (expression.TryExtractQueryable(out var sourceQueryable, typeof(EFQueryableExtensions), typeof(EFRelationalQueryableExtensions)))
            {
                return sourceQueryable.Provider.CreateQuery<T>(expression) switch
                {
                    IAsyncEnumerable<T> asyncEnumerable => asyncEnumerable,
                    var query => query.AsAsyncEnumerable()
                };
            }
#endif
            throw new InvalidOperationException($"Unable to extract EF query provider from {expression}.");
        }

        public Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken)
        {
            // Handle EF Core 6.0
#if NET6_0_OR_GREATER
            if (expression.TryExtractQueryProvider(out var queryProvider, typeof(EFQueryableExtensions), typeof(EFRelationalQueryableExtensions)))
            {
                return queryProvider.ExecuteAsync<Task<T>>(expression, cancellationToken);
            }
            throw new InvalidOperationException($"Unable to extract EF query provider from {expression}.");
#else
            // Handle EF Core 3.1
            if (!expression.TryExtractQueryable(out var queryable, typeof(EFQueryableExtensions), typeof(EFRelationalQueryableExtensions)))
            {
                throw new InvalidOperationException($"Unable to extract queryable from {expression}.");
            }
            if (queryable.Provider is not IEFAsyncQueryProvider provider)
            {
                throw new InvalidOperationException($"Invalid or unhandled EF Core provider of type {queryable.Provider.GetType()}.");
            }
            return provider.ExecuteAsync<Task<T>>(expression, cancellationToken);
#endif
        }
    }
}

#pragma warning restore EF1001