using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace NCoreUtils.Data.EntityFrameworkCore;

public abstract class EntityFrameworkDataRepository : IDataRepository
{
    IDataRepositoryContext IDataRepository.Context => EFCoreContext;

    public abstract Type ElementType { get; }

    public DataRepositoryContext EFCoreContext { get; }

    protected EntityFrameworkDataRepository(DataRepositoryContext efCoreContext)
        => EFCoreContext = efCoreContext ?? throw new ArgumentNullException(nameof(efCoreContext));


}

public abstract class EntityFrameworkDataRepository<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TData>
    : EntityFrameworkDataRepository, IDataRepository<TData>
    where TData : class
{
    public virtual IQueryable<TData> Items => EFCoreContext.DbContext.Set<TData>();

    public override Type ElementType => typeof(TData);

    // protected abstract ValueTask<EntityEntry<TData>> AttachNewOrUpdateAsync(TData entry, CancellationToken cancellationToken);

    public virtual async Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dbContext = EFCoreContext.DbContext;
        var entry = dbContext.Entry(item);
        // Entity is either attached or detached. In later case context may have another instance attached to the same
        // key. If this is the case we reuse the entity tracked by the context by default but this functionality can be
        // altered --> overridable method.
        if (entry.State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {

        }

    }
}