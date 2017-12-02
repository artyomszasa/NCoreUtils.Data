using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    public abstract class DataEventObserver : IDataEventHandler
    {
        protected virtual Task HandleAsync(DataOperation operation, object entity, CancellationToken cancellationToken) => Task.CompletedTask;

        public virtual Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
            => HandleAsync(@event.Operation, @event.Entity, cancellationToken);
    }

    public abstract class DataEventObserver<T> : IDataEventHandler
        where T : class
    {
        protected virtual Task HandleAsync(T entity, CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task HandleAsync(DataOperation operation, T entity, CancellationToken cancellationToken)
            => HandleAsync(entity, cancellationToken);

        protected virtual Task HandleAsync(IDataRepository<T> repository, DataOperation operation, T entity, CancellationToken cancellationToken)
            => HandleAsync(operation, entity, cancellationToken);

        protected virtual Task HandleAsync(IDataEvent<T> @event, CancellationToken cancellationToken)
            => HandleAsync(@event.Repository, @event.Operation, @event.Entity, cancellationToken);

        public virtual Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (@event is IDataEvent<T> e)
            {
                return HandleAsync(e, cancellationToken);
            }
            return Task.CompletedTask;
        }
    }
}