using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Linq;
#if NET6_0_OR_GREATER
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.IAsyncQueryProvider;
#else
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider;
#endif

namespace NCoreUtils.Data.EntityFrameworkCore
{
    internal sealed class QueryProviderAdapter : IAsyncQueryAdapter
    {
        public ValueTask<IAsyncQueryProvider> GetAdapterAsync(Func<ValueTask<IAsyncQueryProvider>> next, IQueryProvider source, CancellationToken cancellationToken)
        {
            if (source is IEFAsyncQueryProvider)
            {
                return new ValueTask<IAsyncQueryProvider>(AdaptedQueryProvider.SharedInstance);
            }
            return next();
        }
    }
}