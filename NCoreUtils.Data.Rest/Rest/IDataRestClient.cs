using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.Rest
{
    public interface IDataRestClient<TData>
    {
        IQueryable<TData> Collection();
    }

    public interface IDataRestClient<TData, TId> : IDataRestClient<TData>
        where TData : IHasId<TId>
    {
        Task<TData?> ItemAsync(TId id, CancellationToken cancellationToken = default);

        Task<TId> CreateAsync(TData data, CancellationToken cancellationToken = default);

        Task UpdateAsync(TId id, TData data, CancellationToken cancellationToken = default);

        Task DeleteAsync(TId id, bool force, CancellationToken cancellationToken = default);

        [Obsolete("Use DeleteAsync(id, force, cancellationToken) instead.")]
        Task DeleteAsync(TId id, CancellationToken cancellationToken = default)
            => DeleteAsync(id, false, cancellationToken);
    }
}