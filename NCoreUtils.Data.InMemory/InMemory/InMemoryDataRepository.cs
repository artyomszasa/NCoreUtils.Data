using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;
using NCoreUtils.Linq;

namespace NCoreUtils.Data.InMemory
{
    public class InMemoryDataRepository
    {
        static InMemoryDataRepository()
        {
            AsyncQueryAdapters.Add(new EnumerableQueryProviderAdapter());
        }
    }

    public class InMemoryDataRepository<TData, TId> : InMemoryDataRepository, IDataRepository<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
    {
        IDataRepositoryContext IDataRepository.Context => Context;

        public IList<TData> Data { get; }

        public IQueryable<TData> Items => Data.AsQueryable();

        public Type ElementType => typeof(TData);

        public InMemoryDataRepositoryContext Context { get; }

        public IDataEventHandlers? Handlers { get; }

        public IServiceProvider ServiceProvider { get; }

        public InMemoryDataRepository(
            IServiceProvider serviceProvider,
            InMemoryDataRepositoryContext context,
            IDataEventHandlers? handlers = default,
            IList<TData>? data = default)
        {
            ServiceProvider = serviceProvider;
            Context = context;
            Handlers = handlers;
            Data = data ?? new List<TData>();
        }

        public Task<TData> LookupAsync(TId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Data.FirstOrDefault(e => e.Id.Equals(id)));
        }

        public Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
        {
            var index = Data.FindIndex(e => e.Id.Equals(item.Id));
            if (-1 == index)
            {
                var property = typeof(TData).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
                if (!(property is null) && property.CanWrite)
                {
                    property.SetValue(item, Data.Count == 0 ? 1 : ((dynamic)Data.Max(e => e.Id) + 1));
                }
                Handlers?.TriggerInsertAsync(ServiceProvider, this, item).AsTask().Wait();
                Data.Add(item);
            }
            else
            {
                Handlers?.TriggerUpdateAsync(ServiceProvider, this, item).AsTask().Wait();
                Data[index] = item;
            }
            return Task.FromResult(item);
        }

        public Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default)
        {
            var index = Data.FindIndex(e => e.Id.Equals(item.Id));
            if (-1 != index)
            {
                Handlers?.TriggerDeleteAsync(ServiceProvider, this, item).AsTask().Wait();
                Data.RemoveAt(index);
            }
            return Task.CompletedTask;
        }
    }
}