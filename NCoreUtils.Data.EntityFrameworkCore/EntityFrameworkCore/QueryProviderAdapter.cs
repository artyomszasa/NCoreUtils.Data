using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Linq;
using IEFAsyncQueryProvider = Microsoft.EntityFrameworkCore.Query.Internal.IAsyncQueryProvider;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    sealed class QueryProviderAdapter : IAsyncQueryAdapter
    {
        public Task<IAsyncQueryProvider> GetAdapterAsync(Func<Task<IAsyncQueryProvider>> next, IQueryProvider source, CancellationToken cancellationToken)
        {
            if (source is IEFAsyncQueryProvider)
            {
                return Task.FromResult<IAsyncQueryProvider>(AdaptedQueryProvider.SharedInstance);
            }
            return next();
        }
    }
}