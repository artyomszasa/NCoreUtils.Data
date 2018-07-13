using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Performes automatic field updates on time tracking fields defined by <c>IHasTimetracking</c>.
    /// </summary>
    [ImplicitDataEventObserver]
    public class TimeTrackingObserver : DataEventObserver
    {
        /// <summary>
        /// Updates fields defined by <c>IHasTimetracking</c> depending on the actual operation.
        /// </summary>
        /// <param name="operation">Actual operation.</param>
        /// <param name="entity">Target entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        protected override Task HandleAsync(DataOperation operation, object entity, CancellationToken cancellationToken)
        {
            if (operation == DataOperation.Insert)
            {
                if (entity is IHasTimeTracking obj)
                {
                    var now = DateTimeOffset.Now.UtcTicks;
                    obj.Created = now;
                    obj.Updated = now;
                }
            }
            else if (operation == DataOperation.Update)
            {
                if (entity is IHasTimeTracking obj)
                {
                    obj.Updated = DateTimeOffset.Now.UtcTicks;
                }
            }
            else if (operation == DataOperation.Delete)
            {
                if (entity is IHasTimeTracking obj && entity is IHasState)
                {
                    obj.Updated = DateTimeOffset.Now.UtcTicks;
                }
            }
            return Task.CompletedTask;
        }
    }
}