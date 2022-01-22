using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.Rest
{
    public abstract class RestDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData>
        : IDataRepository<TData>
    {
        IDataRepositoryContext IDataRepository.Context => Context;

        public abstract IQueryable<TData> Items { get; }

        public Type ElementType => typeof(TData);

        public RestDataRepositoryContext Context { get; }

        public RestDataRepository(RestDataRepositoryContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        protected abstract ValueTask<bool> ShouldUpdate(TData data, CancellationToken cancellationToken);

        public abstract Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default);

        public abstract Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default);
    }

    public class RestDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData, TId>
        : RestDataRepository<TData>, IDataRepository<TData, TId>
        where TData : IHasId<TId>
    {
        public override IQueryable<TData> Items => Client.Collection();

        public IDataRestClient<TData, TId> Client { get; }

        public RestDataRepository(RestDataRepositoryContext context, IDataRestClient<TData, TId> client)
            : base(context)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<TData?> LookupAsync(TId id, CancellationToken cancellationToken = default)
            => Client.ItemAsync(id, cancellationToken);

        protected override ValueTask<bool> ShouldUpdate(TData data, CancellationToken cancellationToken)
            => new(IdUtils.HasValidId(data));

        public override async Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
        {
            TId id;
            if (await ShouldUpdate(item, cancellationToken))
            {
                await Client.UpdateAsync(item.Id, item, cancellationToken);
                id = item.Id;
            }
            else
            {
                id = await Client.CreateAsync(item, cancellationToken);
            }
            return (await LookupAsync(id, cancellationToken))!;
        }

        public override Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default)
        {
            if (!IdUtils.HasValidId(item))
            {
                throw new InvalidOperationException($"Unable to remove entity without valid id.");
            }
            return Client.DeleteAsync(item.Id, force, cancellationToken);
        }
    }
}