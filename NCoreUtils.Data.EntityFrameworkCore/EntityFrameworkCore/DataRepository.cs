using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NCoreUtils.Data.IdNameGeneration;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NCoreUtils.Data.EntityFrameworkCore
{
    /// <summary>
    /// Common implmentation of repository backed by entity framework core context.
    /// </summary>
    public abstract partial class DataRepository : ISupportsIdNameGeneration
    {
        static readonly ConcurrentDictionary<Type, Maybe<(PropertyInfo, ImmutableArray<PropertyInfo>)>> _idNameSourceProperties
            = new ConcurrentDictionary<Type, Maybe<(PropertyInfo, ImmutableArray<PropertyInfo>)>>();

        /// <summary>
        /// Internal Id name descriptions cache.
        /// </summary>
        protected static readonly ConcurrentDictionary<Type, Maybe<IdNameDescription>> _idNameDesciptionCache
            = new ConcurrentDictionary<Type, Maybe<IdNameDescription>>();

        /// <summary>
        /// Default special property names. Spcial properties are not opverridden on entity update.
        /// </summary>
        protected static ImmutableHashSet<string> _defaultSpecialPropertyNames = ImmutableHashSet.CreateRange(StringComparer.InvariantCultureIgnoreCase, new []
        {
            "Created",
            "CreatedById"
        });

        static DataRepository()
        {
            Linq.AsyncQueryAdapters.Add(new QueryProviderAdapter());
        }

        protected static Maybe<(PropertyInfo, ImmutableArray<PropertyInfo>)> MaybeIdNameSourceProperty(Type elementType, DbContext dbContext)
        {
            if (_idNameSourceProperties.TryGetValue(elementType, out var result))
            {
                return result;
            }
            return _idNameSourceProperties.GetOrAdd(elementType, etype => dbContext
                .Model
                .FindEntityType(etype)
                .GetProperties()
                .MaybePick(picker));

            static Maybe<(PropertyInfo, ImmutableArray<PropertyInfo>)> picker(Microsoft.EntityFrameworkCore.Metadata.IProperty e)
            {
                var annotation = e.FindAnnotation(Annotations.IdNameSourceProperty);
                if (null == annotation)
                {
                    return Maybe.Nothing;
                }
                var a = Annotations.IdNameSourcePropertyAnnotation.Unpack(annotation.Value as string);
                return (a.SourceNameProperty, a.AdditionalIndexProperties).Just();
            }
        }

        /// <summary>
        /// Unredlying repository context.
        /// </summary>
        [Obsolete("Use EFCoreContext property instead.")]
        protected readonly DataRepositoryContext _context;

        /// <summary>
        /// Gets element type handled by the repository.
        /// </summary>
        public abstract Type ElementType { get; }

        /// <summary>
        /// Unredlying repository context.
        /// </summary>
        #pragma warning disable 0618
        public DataRepositoryContext EFCoreContext => _context;
        #pragma warning restore 0618

        /// <summary>
        /// Initializes new instance of repository from the specified context.
        /// </summary>
        /// <param name="context"></param>
        public DataRepository(DataRepositoryContext context)
        {
            #pragma warning disable 0618
            _context = context ?? throw new ArgumentNullException(nameof(context));
            #pragma warning restore 0618
        }

        public virtual bool GenerateIdNameOnInsert => MaybeIdNameSourceProperty(ElementType, EFCoreContext.DbContext).HasValue;

        public abstract IdNameDescription IdNameDescription { get; }

        public virtual IStringDecomposer DecomposeName => DummyStringDecomposition.Decomposer;
    }

    public abstract class DataRepository<TData> : DataRepository, IDataRepository<TData>
        where TData : class
    {
        public static IdNameDescription GetIdNameDescription(Type elementType, DbContext dbContext, IStringDecomposer decomposer)
        {
            if (!_idNameDesciptionCache.TryGetValue(elementType, out var desc))
            {
                var maybeSelector = ByIdNameExpressionBuilder.MaybeGetExpression(elementType).As<Expression<Func<TData, string>>>();
                if (!maybeSelector.TryGetValue(out var selector) || !MaybeIdNameSourceProperty(elementType, dbContext).TryGetValue(out var annotation))
                {
                    _idNameDesciptionCache[elementType] = Maybe.Nothing;
                    desc = Maybe.Nothing;
                }
                else
                {
                    var d = new IdNameDescription(selector.ExtractProperty(), annotation.Item1, decomposer, annotation.Item2);
                    _idNameDesciptionCache[elementType] = d.Just();
                    desc = d.Just();
                }
            }
            if (desc.TryGetValue(out var result))
            {
                return result;
            }
            throw new InvalidOperationException($"No id name description for {typeof(TData).FullName}.");
        }

        protected virtual ImmutableHashSet<string> SpecialPropertyNames => _defaultSpecialPropertyNames;

        public virtual IQueryable<TData> Items => EFCoreContext.DbContext.Set<TData>();

        public override Type ElementType => typeof(TData);

        public override IdNameDescription IdNameDescription => GetIdNameDescription(ElementType, EFCoreContext.DbContext, DecomposeName);

        public IDataRepositoryContext Context => EFCoreContext;

        public IServiceProvider ServiceProvider { get; }

        public IDataEventHandlers EventHandlers { get; }

        public DataRepository(IServiceProvider serviceProvider, DataRepositoryContext context, IDataEventHandlers eventHandlers = null)
            : base(context)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            EventHandlers = eventHandlers;
        }

        protected abstract Task<EntityEntry<TData>> AttachNewOrUpdateAsync(EntityEntry<TData> entry, CancellationToken cancellationToken);

        protected virtual async Task PrepareUpdatedEntityAsync(EntityEntry<TData> entry, CancellationToken cancellationToken = default)
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
                            if (p.CurrentValue is null)
                            {
                                p.CurrentValue = originalValues[p.Metadata.Name];
                            }
                        }
                    }
                }
            }
        }

        protected virtual Task PrepareAddedEntityAsync(EntityEntry<TData> entry, CancellationToken cancellationToken = default)
            => EventHandlers.TriggerInsertAsync(ServiceProvider, this, entry.Entity, cancellationToken);

        public virtual async Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dbContext = EFCoreContext.DbContext;
            var entry = dbContext.Entry(item);
            if (entry.State == EntityState.Detached)
            {
                entry = await AttachNewOrUpdateAsync(entry, cancellationToken);
            }
            else if (entry.State == EntityState.Added)
            {
                await EventHandlers.TriggerInsertAsync(ServiceProvider, this, entry.Entity, cancellationToken);
            }
            else if (entry.State == EntityState.Modified)
            {
                await EventHandlers.TriggerUpdateAsync(ServiceProvider, this, entry.Entity, cancellationToken);
            }
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return entry.Entity;
        }

        public virtual Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dbContext = EFCoreContext.DbContext;
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
            catch (Exception exn)
            {
                return Task.FromException(exn);
            }
        }
    }

    public class DataRepository<TData, TId> : DataRepository<TData>, IDataRepository<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IComparable<TId>
    {
        public DataRepository(IServiceProvider serviceProvider, DataRepositoryContext context, IDataEventHandlers eventHandlers = null) : base(serviceProvider, context, eventHandlers) { }

        protected override async Task<EntityEntry<TData>> AttachNewOrUpdateAsync(EntityEntry<TData> entry, CancellationToken cancellationToken)
        {
            var dbContext = EFCoreContext.DbContext;
            if (entry.Entity.Id.CompareTo(default) > 0)
            {
                // check whether another instance is already tracked
                var existentEntry = dbContext.ChangeTracker.Entries<TData>().FirstOrDefault(e => e.Entity.Id.Equals(entry.Entity.Id));
                if (null == existentEntry)
                {
                    var updatedEntry = dbContext.Update(entry.Entity);
                    await PrepareUpdatedEntityAsync(updatedEntry, cancellationToken).ConfigureAwait(false);
                    return updatedEntry;
                }
                existentEntry.CurrentValues.SetValues(entry.Entity);
                await PrepareUpdatedEntityAsync(existentEntry, cancellationToken).ConfigureAwait(false);
                return existentEntry;
            }
            var addedEntry = await dbContext.AddAsync(entry.Entity, cancellationToken).ConfigureAwait(false);
            await PrepareAddedEntityAsync(addedEntry, cancellationToken);
            return addedEntry;
        }

        public virtual Task<TData> LookupAsync(TId id, CancellationToken cancellationToken = default)
            => Items.Where(ByIdExpressionBuilder<TData, TId>.CreateFilter(id)).FirstOrDefaultAsync(cancellationToken);
    }
}