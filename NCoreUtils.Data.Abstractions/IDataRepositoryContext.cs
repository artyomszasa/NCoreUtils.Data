using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.Data
{
    /// <summary>
    /// Defines data context functionality. Data context is shared between data repositories that uses same data
    /// source.
    /// </summary>
    public interface IDataRepositoryContext : IDisposable
    {
        /// <summary>
        /// Gets current transaction. Returns <c>null</c> if no transaction is active.
        /// </summary>
        IDataTransaction? CurrentTransaction { get; }

        /// <summary>
        /// Starts new transaction.
        /// </summary>
        /// <param name="isolationLevel">Isolation level.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Started transaction.</returns>
        ValueTask<IDataTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
    }
}