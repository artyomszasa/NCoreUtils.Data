using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Linq
{
    public interface IAsyncQueryProvider
    {
        IAsyncEnumerable<T> ExecuteAsync<T>(Expression expression);

        Task<T> ExecuteAsync<T>(Expression expression, CancellationToken cancellationToken);
    }
}