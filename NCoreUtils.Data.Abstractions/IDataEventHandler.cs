using System.Threading;
using System.Threading.Tasks;
using NCoreUtils.Data.Events;

namespace NCoreUtils.Data
{
    public interface IDataEventHandler
    {
        Task HandleAsync(IDataEvent @event, CancellationToken cancellationToken = default(CancellationToken));
    }
}