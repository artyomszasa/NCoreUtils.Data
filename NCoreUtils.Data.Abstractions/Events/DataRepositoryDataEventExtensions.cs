using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.Events
{
    public static class DataRepositoryDataEventExtensions
    {
        static async Task TriggerAsync<T>(IDataEventHandlers handlers, IDataEvent<T> @event, CancellationToken cancellationToken)
            where T : class
        {
            foreach (var handler in handlers.Handlers)
            {
                await handler.HandleAsync(@event, cancellationToken);
            }
        }

        public static Task TriggerUpdateAsync<T>(this IDataEventHandlers handlers, IServiceProvider serviceProvider, IDataRepository<T> repository, T entity, CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            if (handlers == null)
            {
                return Task.CompletedTask;
            }
            return TriggerAsync(handlers, new DataUpdateEvent<T>(serviceProvider, repository, entity), cancellationToken);
        }

        public static Task TriggerInsertAsync<T>(this IDataEventHandlers handlers, IServiceProvider serviceProvider, IDataRepository<T> repository, T entity, CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            if (handlers == null)
            {
                return Task.CompletedTask;
            }
            return TriggerAsync(handlers, new DataInsertEvent<T>(serviceProvider, repository, entity), cancellationToken);
        }

        public static Task TriggerDeleteAsync<T>(this IDataEventHandlers handlers, IServiceProvider serviceProvider, IDataRepository<T> repository, T entity, CancellationToken cancellationToken = default(CancellationToken))
            where T : class
        {
            if (handlers == null)
            {
                return Task.CompletedTask;
            }
            return TriggerAsync(handlers, new DataDeleteEvent<T>(serviceProvider, repository, entity), cancellationToken);
        }
    }
}