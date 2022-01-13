using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        protected static T Rebox<T>(object o)
            => (T)o;
    }

    public class InMemoryDataRepository<TData, TId> : InMemoryDataRepository, IDataRepository<TData, TId>
        where TData : class, IHasId<TId>
        where TId : IEquatable<TId>
    {
        private static TId Inc(object id)
            => id switch
            {
                short s => Rebox<TId>(s + 1),
                int i => Rebox<TId>(i + 1),
                long l => Rebox<TId>(l + 1),
                _ => throw new InvalidOperationException($"Id type is not supported {typeof(TId)}.")
            };

        IDataRepositoryContext IDataRepository.Context => Context;

        public IList<TData> Data { get; }

        public IQueryable<TData> Items
        {
            [RequiresUnreferencedCode("Enumerating in-memory collections as IQueryable can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.")]
            get => Data.AsQueryable();
        }

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

        public Task<TData?> LookupAsync(TId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Data.FirstOrDefault(e => e.Id.Equals(id)))!;
        }

        public Task<TData> PersistAsync(TData item, CancellationToken cancellationToken = default)
        {
            var index = Data.FindIndex(e => e.Id.Equals(item.Id));
            if (-1 == index)
            {
                var property = typeof(TData).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
                if (property is not null && property.CanWrite)
                {
                    property.SetValue(item, Data.Count == 0 ? 1 : Inc(Data.Max(e => e.Id)!));
                }
                Handlers?.TriggerInsertAsync(ServiceProvider, this, item, cancellationToken)
                    .AsTask()
                    .Wait(CancellationToken.None);
                Data.Add(item);
            }
            else
            {
                Handlers?.TriggerUpdateAsync(ServiceProvider, this, item, cancellationToken)
                    .AsTask()
                    .Wait(CancellationToken.None);
                Data[index] = item;
            }
            return Task.FromResult(item);
        }

        public Task RemoveAsync(TData item, bool force = false, CancellationToken cancellationToken = default)
        {
            var index = Data.FindIndex(e => e.Id.Equals(item.Id));
            if (-1 != index)
            {
                Handlers?.TriggerDeleteAsync(ServiceProvider, this, item, cancellationToken)
                    .AsTask()
                    .Wait(CancellationToken.None);
                Data.RemoveAt(index);
            }
            return Task.CompletedTask;
        }
    }
}