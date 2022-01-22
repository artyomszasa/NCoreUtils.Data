using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Rest;

namespace NCoreUtils.Data.Rest
{
    public class DataRestClient<TData, TId> : IDataRestClient<TData, TId>
        where TData : IHasId<TId>
    {
        private readonly IRestClient _client;

        public DataRestClient(IRestClient client)
            => _client = client ?? throw new ArgumentNullException(nameof(client));

        public IQueryable<TData> Collection() => _client.Collection<TData>();

        public Task<TId> CreateAsync(TData data, CancellationToken cancellationToken = default)
            => _client.CreateAsync<TData, TId>(data, cancellationToken);

        public Task DeleteAsync(TId id, bool force, CancellationToken cancellationToken = default)
            => _client.DeleteAsync<TData, TId>(id, force, cancellationToken);

        public Task<TData?> ItemAsync(TId id, CancellationToken cancellationToken = default)
            => _client.ItemAsync<TData, TId>(id, cancellationToken);

        public Task UpdateAsync(TId id, TData data, CancellationToken cancellationToken = default)
            => _client.UpdateAsync(id, data, cancellationToken);
    }
}