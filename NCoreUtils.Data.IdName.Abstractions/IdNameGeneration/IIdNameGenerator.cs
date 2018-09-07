using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.IdNameGeneration
{
    public interface IIdNameGenerator
    {
        Task<string> GenerateAsync<T>(
            IQueryable<T> directQuery,
            IdNameDescription idNameDescription,
            string name,
            object indexValues = null,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : class, IHasIdName;

        Task<string> GenerateAsync<T>(
            IQueryable<T> directQuery,
            IdNameDescription idNameDescription,
            T entity,
            CancellationToken cancellationToken = default(CancellationToken))
            where T : class, IHasIdName;
    }
}