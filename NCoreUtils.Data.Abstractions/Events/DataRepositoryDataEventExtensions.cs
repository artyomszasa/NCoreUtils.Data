using System;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data.Events
{
    /// <summary>
    /// Provides extensions for triggering data repository related events.
    /// </summary>
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

        /// <summary>
        /// Initializes and triggers update event.
        /// </summary>
        /// <param name="handlers">Data event handler implementation.</param>
        /// <param name="serviceProvider">Service provider of the current context.</param>
        /// <param name="repository">Data repository triggering the event.</param>
        /// <param name="entity">Related entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task TriggerUpdateAsync<T>(
            this IDataEventHandlers handlers,
            IServiceProvider serviceProvider,
            IDataRepository<T> repository,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (handlers == null)
            {
                return Task.CompletedTask;
            }
            return TriggerAsync(handlers, new DataUpdateEvent<T>(serviceProvider, repository, entity), cancellationToken);
        }

        /// <summary>
        /// Initializes and triggers insert event.
        /// </summary>
        /// <param name="handlers">Data event handler implementation.</param>
        /// <param name="serviceProvider">Service provider of the current context.</param>
        /// <param name="repository">Data repository triggering the event.</param>
        /// <param name="entity">Related entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task TriggerInsertAsync<T>(
            this IDataEventHandlers handlers,
            IServiceProvider serviceProvider,
            IDataRepository<T> repository,
            T entity,
            CancellationToken cancellationToken = default)
            where T : class
        {
            if (handlers == null)
            {
                return Task.CompletedTask;
            }
            return TriggerAsync(handlers, new DataInsertEvent<T>(serviceProvider, repository, entity), cancellationToken);
        }

        /// <summary>
        /// Initializes and triggers delete event.
        /// </summary>
        /// <param name="handlers">Data event handler implementation.</param>
        /// <param name="serviceProvider">Service provider of the current context.</param>
        /// <param name="repository">Data repository triggering the event.</param>
        /// <param name="entity">Related entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static Task TriggerDeleteAsync<T>(
            this IDataEventHandlers handlers,
            IServiceProvider serviceProvider,
            IDataRepository<T> repository,
            T entity,
            CancellationToken cancellationToken = default)
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