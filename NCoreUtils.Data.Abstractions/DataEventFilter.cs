using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Data repository related event handler that passes filtered events to another handler.
    /// </summary>
    public abstract class DataEventFilter : IDataEventHandler
    {
        sealed class ExplicitDataEventFilter : DataEventFilter
        {
            public Func<IDataEvent, CancellationToken, Task<bool>> Predicate { get; }

            public ExplicitDataEventFilter(
                IDataEventHandler handler,
                Func<IDataEvent, CancellationToken, Task<bool>> predicate)
                : base(handler)
                => Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

            protected override Task<bool> IsHandled(IDataEvent @event, CancellationToken cancellationToken)
                => Predicate(@event, cancellationToken);
        }

        /// <summary>
        /// Creates new data repository related event handler that passes events that satisfies
        /// <paramref name="predicate" /> to data event handler specified by <paramref name="handler" />.
        /// </summary>
        /// <param name="handler">Target handler.</param>
        /// <param name="predicate">Predicate used to filter data events.</param>
        /// <returns>Newly created data event handler.</returns>
        public static DataEventFilter Filter(
            IDataEventHandler handler,
            Func<IDataEvent, CancellationToken, Task<bool>> predicate)
            => new ExplicitDataEventFilter(handler, predicate);

        /// <summary>
        /// Gets target handler of the current instance.
        /// </summary>
        public IDataEventHandler Handler { get; }

        /// <summary>
        /// Initializes new instance with the specified target handler.
        /// </summary>
        /// <param name="handler">Target handler.</param>
        protected DataEventFilter(IDataEventHandler handler)
            => Handler = handler ?? throw new System.ArgumentNullException(nameof(handler));

        /// <summary>
        /// Performes user defined operation for single data repository related data event. Overridden to invoke target
        /// handler only if the data event is handled.
        /// </summary>
        /// <param name="event">Data event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default)
        {
            if (await IsHandled(@event, cancellationToken))
            {
                await Handler.HandleAsync(@event, cancellationToken);
            }
        }

        /// <summary>
        /// Determines whether the specified data event should be handled i.e. passed to the target handler.
        /// </summary>
        /// <param name="event"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// <c>true</c> if the specified data event should be handled, <c>false</c> otherwise.
        /// </returns>
        protected abstract Task<bool> IsHandled(IDataEvent @event, CancellationToken cancellationToken);
    }
}