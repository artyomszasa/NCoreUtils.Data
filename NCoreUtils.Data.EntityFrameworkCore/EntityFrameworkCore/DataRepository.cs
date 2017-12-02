using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    public abstract partial class DataRepository
    {
        protected static ImmutableHashSet<string> _defaultSpecialPropertyNames = ImmutableHashSet.CreateRange(StringComparer.InvariantCultureIgnoreCase, new []
        {
            "Created",
            "CreatedById"
        });

        static DataRepository()
        {
            Linq.AsyncQueryAdapters.Add(new QueryProviderAdapter());
        }
    }

    public abstract class DataRepository<TData> : DataRepository, IDataRepository<TData>
        where TData : class
    {
        protected readonly DataRepositoryContext _context;

        protected virtual ImmutableHashSet<string> SpecialPropertyNames => _defaultSpecialPropertyNames;

        public virtual IQueryable<TData> Items => _context.DbContext.Set<TData>();

        public Type ElementType => typeof(TData);

        public IDataRepositoryContext Context => _context;

        public IServiceProvider ServiceProvider { get; }

        public IDataEventHandlers EventHandlers { get; }

        public DataRepository(IServiceProvider serviceProvider, DataRepositoryContext context, IDataEventHandlers eventHandlers = null)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            EventHandlers = eventHandlers;
        }

        protected abstract Task<EntityEntry<TData>> AttachNewOrUpdateAsync(EntityEntry<TData> entry, CancellationToken cancellationToken);

        protected virtual async Task PrepareUpdatedEntityAsync(EntityEntry<TData> entry, CancellationToken cancellationToken = default(CancellationToken))
        {
            await EventHandlers.TriggerUpdateAsync(ServiceProvider, this, entry.Entity, cancellationToken);
            // Speciális mezőket nem kell frissíteni...
            var originalValues = await entry.GetDatabaseValuesAsync(cancellationToken);
            foreach (var p in entry.Properties)
            {
                if (SpecialPropertyNames.Contains(p.Metadata.Name))
                {
                    if (!p.CurrentValue.Equals(originalValues[p.Metadata.Name]))
                    {
                        if (p.Metadata.ClrType.IsValueType)
                        {
                            var defValue = Activator.CreateInstance(p.Metadata.ClrType);
                            if (defValue.Equals(p.CurrentValue))
                            {
                                p.CurrentValue = originalValues[p.Metadata.Name];
                            }
                        }
                        else
                        {
                            if (ReferenceEquals(null, p.CurrentValue))
                            {
                                p.CurrentValue = originalValues[p.Metadata.Name];
                            }
                        }
                    }
                }
            }
        }

        protected virtual Task PrepareAddedEntityAsync(EntityEntry<TData> entry, CancellationToken cancellationToken = default(CancellationToken))
            => EventHandlers.TriggerInsertAsync(ServiceProvider, this, entry.Entity, cancellationToken);

        public virtual async Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dbContext = _context.DbContext;
            var entry = dbContext.Entry(item);
            if (entry.State == EntityState.Detached)
            {
                entry = await AttachNewOrUpdateAsync(entry, cancellationToken);
            }
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entry.Entity;
        }

        public virtual Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dbContext = _context.DbContext;
            var entry = dbContext.Entry(item);
            if (entry.State == EntityState.Detached)
            {
                throw new InvalidOperationException("Trying to remove detached entity.");
            }
            EventHandlers.TriggerDeleteAsync(ServiceProvider, this, entry.Entity, cancellationToken);
            if (!force && item is IHasState statefullEntity)
            {
                statefullEntity.State = State.Deleted;
            }
            else
            {
                dbContext.Remove(item);
            }
            return dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public class DataRepository<TData, TId> : DataRepository<TData>, IDataRepository<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IComparable<TId>
    {
        public DataRepository(IServiceProvider serviceProvider, DataRepositoryContext context, IDataEventHandlers eventHandlers = null) : base(serviceProvider, context, eventHandlers) { }

        protected override async Task<EntityEntry<TData>> AttachNewOrUpdateAsync(EntityEntry<TData> entry, CancellationToken cancellationToken)
        {
            var dbContext = _context.DbContext;
            if (entry.Entity.Id.CompareTo(default(TId)) > 0)
            {
                var updatedEntry = dbContext.Update(entry.Entity);
                await PrepareUpdatedEntityAsync(updatedEntry, cancellationToken).ConfigureAwait(false);
                return updatedEntry;
            }
            var addedEntry = await dbContext.AddAsync(entry.Entity, cancellationToken).ConfigureAwait(false);
            await PrepareAddedEntityAsync(addedEntry, cancellationToken);
            return addedEntry;
        }

        public virtual Task<TData> LookupAsync(TId id, CancellationToken cancellationToken = default(CancellationToken))
            => Items.Where(ByIdExpressionBuilder<TData, TId>.CreateFilter(id)).FirstOrDefaultAsync(cancellationToken);
    }
}