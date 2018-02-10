using System;
using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    /// <summary>
    ///
    /// </summary>
    [ImplicitDataEventObserver]
    public class TimeTrackingObserver : DataEventObserver
    {
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