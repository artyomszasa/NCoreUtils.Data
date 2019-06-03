using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Defines functionality for handling data repository related events.
    /// </summary>
    public interface IDataEventHandler
    {
        /// <summary>
        /// Performes user defined operation for single data repository related data event.
        /// </summary>
        /// <param name="event">Data event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default);
    }
}