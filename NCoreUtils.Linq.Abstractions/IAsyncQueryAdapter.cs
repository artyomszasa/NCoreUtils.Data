using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    public interface IAsyncQueryAdapter
    {
        Task<IAsyncQueryProvider> GetAdapterAsync(Func<Task<IAsyncQueryProvider>> next, IQueryProvider source, CancellationToken cancellationToken);
    }
}