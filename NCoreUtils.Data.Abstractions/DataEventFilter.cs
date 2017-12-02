using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    public abstract class DataEventFilter : IDataEventHandler
    {
        sealed class ExplicitDataEventFilter : DataEventFilter
        {
            public Func<IDataEvent, CancellationToken, Task<bool>> Predicate { get; }

            public ExplicitDataEventFilter(IDataEventHandler handler, Func<IDataEvent, CancellationToken, Task<bool>> predicate)
                : base(handler)
                => Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

            protected override Task<bool> IsHandled(IDataEvent @event, CancellationToken cancellationToken) => Predicate(@event, cancellationToken);
        }

        public static DataEventFilter Filter(IDataEventHandler handler, Func<IDataEvent, CancellationToken, Task<bool>> predicate)
            => new ExplicitDataEventFilter(handler, predicate);

        public IDataEventHandler Handler { get; }

        protected DataEventFilter(IDataEventHandler handler) => Handler = handler ?? throw new System.ArgumentNullException(nameof(handler));

        public async Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (await IsHandled(@event, cancellationToken))
            {
                await Handler.HandleAsync(@event, cancellationToken);
            }
        }

        protected abstract Task<bool> IsHandled(IDataEvent @event, CancellationToken cancellationToken);
    }
}