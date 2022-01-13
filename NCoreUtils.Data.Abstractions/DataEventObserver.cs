using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Base class of the generic data event observer.
    /// </summary>
    public abstract class DataEventObserver : IDataEventHandler
    {
        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="operation">Operation being performed.</param>
        /// <param name="entity">Data entity the operation being performed on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected virtual ValueTask HandleAsync(DataOperation operation, object entity, CancellationToken cancellationToken) => default;

        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="event">Data event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
            => HandleAsync(@event.Operation, @event.Entity, cancellationToken);
    }

    /// <summary>
    /// Base class of the typed data event observer.
    /// </summary>
    public abstract class DataEventObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : IDataEventHandler
        where T : class
    {
        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="entity">Data entity the operation being performed on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected virtual ValueTask HandleAsync(T entity, CancellationToken cancellationToken) => default;

        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="operation">Operation being performed.</param>
        /// <param name="entity">Data entity the operation being performed on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected virtual ValueTask HandleAsync(DataOperation operation, T entity, CancellationToken cancellationToken)
            => HandleAsync(entity, cancellationToken);

        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="repository">Repository triggering the event.</param>
        /// <param name="operation">Operation being performed.</param>
        /// <param name="entity">Data entity the operation being performed on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected virtual ValueTask HandleAsync(IDataRepository<T> repository, DataOperation operation, T entity, CancellationToken cancellationToken)
            => HandleAsync(operation, entity, cancellationToken);

        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="event">Data event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected virtual ValueTask HandleAsync(IDataEvent<T> @event, CancellationToken cancellationToken)
            => HandleAsync(@event.Repository, @event.Operation, @event.Entity, cancellationToken);

        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="event">Data event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual ValueTask HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event is IDataEvent<T> e)
            {
                return HandleAsync(e, cancellationToken);
            }
            return default;
        }
    }
}