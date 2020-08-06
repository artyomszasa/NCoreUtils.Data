using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Linq;
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider;

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